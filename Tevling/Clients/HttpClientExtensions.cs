using Polly;
using Polly.Extensions.Http;

namespace Tevling.Clients;

public static class HttpClientExtensions
{
    public static IHttpClientBuilder AddStravaClient(this IServiceCollection services)
        => services
            .AddHttpClient<IStravaClient, StravaClient>()
            // https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler(
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));
}
