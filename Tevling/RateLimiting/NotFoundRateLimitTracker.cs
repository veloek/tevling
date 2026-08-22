using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Tevling.RateLimiting;

public class NotFoundRateLimitTracker(
    IDistributedCache cache,
    IOptions<NotFoundRateLimitOptions> options)
{
    private const string KeyPrefix = "not-found-rate-limit:";
    private readonly NotFoundRateLimitOptions _options = options.Value;

    public async Task<bool> IsExceededAsync(
        string clientKey,
        CancellationToken cancellationToken = default)
    {
        string key = KeyPrefix + clientKey;
        DateTime cutoff = DateTime.UtcNow - _options.Window;

        byte[]? existing = await cache.GetAsync(key, cancellationToken);
        if (existing is null) return false;

        List<DateTime>? timestamps = JsonSerializer.Deserialize<List<DateTime>>(existing);
        if (timestamps is null) return false;

        return timestamps.Count(t => t >= cutoff) > _options.MaxNotFoundRequests;
    }

    public async Task RegisterNotFoundAsync(
        string clientKey,
        CancellationToken cancellationToken = default)
    {
        string key = KeyPrefix + clientKey;
        DateTime now = DateTime.UtcNow;
        DateTime cutoff = now - _options.Window;

        byte[]? existing = await cache.GetAsync(key, cancellationToken);
        List<DateTime> timestamps = existing is null
            ? []
            : JsonSerializer.Deserialize<List<DateTime>>(existing) ?? [];

        timestamps.RemoveAll(t => t < cutoff);
        timestamps.Add(now);

        await cache.SetAsync(
            key,
            JsonSerializer.SerializeToUtf8Bytes(timestamps),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _options.Window },
            cancellationToken);
    }
}
