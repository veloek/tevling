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
    private static readonly string PROFILES_DIR = Path.Join(Environment.CurrentDirectory, "storage", "profiles");
    private static readonly TimeSpan CACHE_MAXAGE = TimeSpan.FromDays(1);
    private const string PROFILE_FALLBACK = "/avatar/athlete/large.png";

    [HttpGet]
    [Route("{athleteId}")]
    public async Task<IActionResult> GetProfilePicture([FromRoute] int athleteId)
    {
        Athlete? athlete = await athleteService.GetAthleteByIdAsync(athleteId, HttpContext.RequestAborted);
        if (athlete is null)
            return NotFound();

        if (athlete.ImgUrl is null)
            return LocalRedirect(PROFILE_FALLBACK);

        // Check if profile picture is already cached (and not too old)
        (FileInfo? cachedProfile, string contentType) = GetCachedProfile(athleteId);
        if (cachedProfile is not null)
        {
            TimeSpan age = DateTime.UtcNow - cachedProfile.CreationTimeUtc;
            if (age < CACHE_MAXAGE)
            {
                FileStream profile = cachedProfile.OpenRead();

                return File(profile, contentType);
            }
        }

        // Fetch profile picture and store it
        logger.LogDebug("Fetching profile picture for athlete: {AthleteId}", athleteId);
        try
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            HttpResponseMessage response = await httpClient.GetAsync(athlete.ImgUrl);
            contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

            byte[] data = await response.Content.ReadAsByteArrayAsync(HttpContext.RequestAborted);

            await StoreProfile(athleteId, data, contentType);

            return File(data, contentType);
        }
        catch (Exception e)
        {
            if (cachedProfile is null)
            {
                logger.LogError(e, "Unable to download profile picture, using fallback.");

                return LocalRedirect(PROFILE_FALLBACK);
            }
            else
            {
                logger.LogError(e, "Unable to download profile picture, using outdated cache.");
                FileStream profile = cachedProfile.OpenRead();

                return File(profile, contentType);
            }
        }
    }

    private static (FileInfo? file, string contentType) GetCachedProfile(int athleteId)
    {
        DirectoryInfo profilesDir = new(PROFILES_DIR);
        if (!profilesDir.Exists)
            profilesDir.Create();

        FileInfo? file = profilesDir.EnumerateFiles()
            .FirstOrDefault(f => f.Name.StartsWith($"{athleteId}."));

        if (file is null)
            return (null, "");

        if (file.Extension == ".png")
            return (file, "image/png");

        return (file, "image/jpeg");
    }

    private static async Task StoreProfile(int athleteId, ReadOnlyMemory<byte> image, string contentType)
    {
        string ext = contentType == "image/png" ? "png" : "jpg";
        string filename = $"{athleteId}.{ext}";
        string path = Path.Join(PROFILES_DIR, filename);

        await System.IO.File.WriteAllBytesAsync(path, image);
    }
}
