using System.Globalization;
using Microsoft.JSInterop;

namespace Tevling.Services;

public class BrowserTime(IJSRuntime jsRuntime, ILogger<BrowserTime> logger) : IBrowserTime, IAsyncDisposable
{
    private readonly ILogger _logger = logger;
    private IJSObjectReference? _module;

    public async Task<DateTimeOffset> ConvertToLocal(DateTimeOffset dt, CancellationToken ct = default)
    {
        _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", ct, "./browser-time.js");

        // This format must be supported by the Date constructor in Javascript.
        // It must also match what the script browser-time.js is returning.
        string format = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffzzz";

        string input = dt.ToString(format);
        string? output = null;
        try
        {
            output = await _module.InvokeAsync<string?>("convert", input);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Error converting DateTime to browser local time");
        }

        if (string.IsNullOrEmpty(output))
        {
            throw new ArgumentException("Unable to convert DateTime: " + dt, nameof(dt));
        }

        DateTimeOffset result = DateTimeOffset.ParseExact(output, format, CultureInfo.InvariantCulture);

        return result;
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
            _module = null;
        }
    }
}
