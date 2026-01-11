using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Tevling.Controllers;

[ApiController]
[Route("profile")]
public class ProfileController(IAthleteService athleteService, IHttpClientFactory httpClientFactory) : ControllerBase
{
    private const int DayInSeconds = 24 * 60 * 60;
    private const string FallbackPicture = "/avatar/athlete/large.png";

    [HttpGet]
    [Route("{athleteId}")]
    [OutputCache(Duration = DayInSeconds)]
    [ResponseCache(Duration = DayInSeconds)]
    public async Task<IActionResult> GetProfilePicture([FromRoute] int athleteId)
    {
        Athlete? athlete = await athleteService.GetAthleteByIdAsync(athleteId, HttpContext.RequestAborted);

        if (athlete is null)
            return NotFound();

        if (!Uri.IsWellFormedUriString(athlete.ImgUrl, UriKind.Absolute))
            return LocalRedirect(FallbackPicture);

        using HttpClient httpClient = httpClientFactory.CreateClient();
        HttpResponseMessage response = (await httpClient.GetAsync(athlete.ImgUrl, HttpContext.RequestAborted))
            .EnsureSuccessStatusCode();

        byte[] data = await response.Content.ReadAsByteArrayAsync(HttpContext.RequestAborted);
        string? contentType = response.Content.Headers.ContentType?.MediaType;

        // If content type is not provided, guess based on file extension.
        contentType ??= athlete.ImgUrl.EndsWith(".png") ? "image/png" : "image/jpeg";

        return File(data, contentType);
    }
}
