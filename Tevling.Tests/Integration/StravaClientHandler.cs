using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tevling.Integration;

public class StravaClientHandler() : HttpMessageHandler
{
    private Func<HttpResponseMessage> _response = () => new(HttpStatusCode.NotFound);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_response());
    }

    public StravaClientHandler WithResponse(Func<HttpResponseMessage> response)
    {
        _response = response;
        return this;
    }
}
