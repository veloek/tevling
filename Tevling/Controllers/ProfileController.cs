using Microsoft.AspNetCore.Mvc;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Tevling.Controllers;

[ApiController]
[Route("profile")]
public class ProfileController(
    ILogger<ProfileController> logger,
    IAthleteService athleteService,
    IHttpClientFactory httpClientFactory)
    : ControllerBase
{
    private static readonly string profilesDir = Path.Join(Environment.CurrentDirectory, "storage", "profiles");

    [HttpGet]
    [Route("{athleteId}")]
    public async Task<IActionResult> GetProfilePicture([FromRoute] int athleteId)
    {
        Athlete? athlete = await athleteService.GetAthleteByIdAsync(athleteId, HttpContext.RequestAborted);
        if (athlete is null)
            return NotFound();

        if (athlete.ImgUrl is null)
            return LocalRedirect("/avatar/athlete/large.png");

        // Check if profile picture is already cached
        (string? cachedProfile, string contentType) = GetCachedProfile(athleteId);
        if (cachedProfile is not null)
        {
            FileStream profile = System.IO.File.OpenRead(Path.Join(profilesDir, cachedProfile));
            return File(profile, contentType);
        }

        // Fetch profile picture and store it
        using HttpClient httpClient = httpClientFactory.CreateClient();
        HttpResponseMessage response = await httpClient.GetAsync(athlete.ImgUrl);
        contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        string ext = contentType == "image/png" ? "png" : "jpg";
        string filename = $"{athleteId}.{ext}";
        string path = Path.Join(profilesDir, filename);
        byte[] data = await response.Content.ReadAsByteArrayAsync(HttpContext.RequestAborted);

        await StoreProfile(path, data);

        return File(data, contentType);
    }

    static private (string? file, string contentType) GetCachedProfile(int athleteId)
    {
        Directory.CreateDirectory(profilesDir);

        string? filename = Directory.EnumerateFiles(profilesDir)
            .Select(fullpath => Path.GetFileName(fullpath))
            .FirstOrDefault(filename => filename.StartsWith($"{athleteId}."));

        if (filename is null)
            return (null, "");

        if (filename.EndsWith(".png"))
            return (filename, "image/png");

        return (filename, "image/jpeg");
    }

    private async Task StoreProfile(string path, ReadOnlyMemory<byte> image)
    {
        try
        {
            await System.IO.File.WriteAllBytesAsync(path, image);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to store profile picture");
            try
            {
                System.IO.File.Delete(path);
            }
            catch (Exception ex2)
            {
                logger.LogError(ex2, "Unable to delete newly created file");
            }
        }
    }
}
