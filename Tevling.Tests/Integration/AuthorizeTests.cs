using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Tevling.Utils;
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

    [Fact]
    public async Task Authorize_should_decode_state_and_redirect_to_returnUrl()
    {
        Strava.TokenResponse tokenResponse = new()
        {
            Athlete = new Strava.SummaryAthlete()
        };

        HttpClient client = _factory
            .WithStravaClientHandler(handler => handler
                .WithResponse(() =>
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(tokenResponse),
                            new MediaTypeHeaderValue("application/json")),
                    }
                ))
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        string state = "?returnUrl=%2Ftest".ToBase64();

        HttpResponseMessage response = await client.GetAsync(
            "/api/authorize?code=123abc&state=" + state, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.ToString().ShouldBe("/test");
    }

    [Fact]
    public async Task Authorize_should_decode_state_and_redirect_to_host()
    {
        Strava.TokenResponse tokenResponse = new()
        {
            Athlete = new Strava.SummaryAthlete()
        };

        HttpClient client = _factory
            .WithStravaClientHandler(handler => handler
                .WithResponse(() =>
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(tokenResponse),
                            new MediaTypeHeaderValue("application/json")),
                    }
                ))
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        string state = $"?host={TevlingWebApplicationFactory.WhitelistedHost}".ToBase64();

        HttpResponseMessage response = await client.GetAsync(
            "/api/authorize?code=123abc&state=" + state, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.Host.ShouldBe(TevlingWebApplicationFactory.WhitelistedHost);
    }
}
