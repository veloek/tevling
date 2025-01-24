using Polly;
using Polly.Extensions.Http;

namespace Tevling.Clients;

public static class HttpClientExtensions
{
    public static IHttpClientBuilder AddStravaClient(this IServiceCollection services)
        => services
            .AddHttpClient<IStravaClient, StravaClient>()
            .AddPolicyHandler(
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));
}
