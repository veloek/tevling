using System.Security.Claims;

namespace Tevling.Authentication;

public class TokenRefreshMiddleware(
    IAthleteService athleteService,
    ILogger<TokenRefreshMiddleware> logger)
    : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Filter on request path to run only once for each page visit
        if (context.Request.Path != "/_blazor") goto next;

        string? athleteIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(athleteIdStr, out int athleteId)) goto next;

        // Calling GetAccessToken will automatically refresh the token if it's
        // close to expiry. Easier than doing the same check here...
        // We don't need to wait for the response though, so just fire-and-forget.
        logger.LogDebug("Checking access token expiry");
        _ = athleteService.GetAccessTokenAsync(athleteId, context.RequestAborted)
            .ContinueWith(async task =>
            {
                // After it's done, await the task to see if it failed
                // and if so, log the error.
                try
                {
                    await task;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Unable to refresh access token");
                }
            });


    next:
        await next(context);
    }
}
