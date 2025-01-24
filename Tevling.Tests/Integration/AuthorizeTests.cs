using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Tevling.Integration;

public class AuthorizeTests
{
    private readonly TevlingWebApplicationFactory _factory = new();

    [Fact]
    public async Task Authorize_should_retry_strava_client_on_failure()
    {
        Strava.TokenResponse tokenResponse = new()
        {
            Athlete = new Strava.SummaryAthlete()
        };

        bool firstAttempt = true;

        HttpClient client = _factory
            .WithStravaClientHandler(handler => handler
                .WithResponse(() =>
                {
                    if (firstAttempt)
                    {
                        firstAttempt = false;
                        return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                    }

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(tokenResponse),
                            new MediaTypeHeaderValue("application/json")),
                    };
                }))
            .CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/authorize?code=123abc", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        firstAttempt.ShouldBeFalse();
    }
}
