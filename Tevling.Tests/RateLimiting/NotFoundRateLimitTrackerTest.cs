using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Tevling.RateLimiting;

public class NotFoundRateLimitTrackerTest
{
    private const string ClientKey = "127.0.0.1";
    private const string CacheKey = "not-found-rate-limit:" + ClientKey;

    [Fact]
    public async Task IsExceededAsync_Should_Return_False_When_No_Entries_Exist()
    {
        IDistributedCache cache = CreateCache();
        NotFoundRateLimitTracker sut = CreateSut(cache);

        bool exceeded = await sut.IsExceededAsync(ClientKey, TestContext.Current.CancellationToken);

        exceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task IsExceededAsync_Should_Return_False_When_Below_Limit()
    {
        IDistributedCache cache = CreateCache();
        NotFoundRateLimitTracker sut = CreateSut(cache, maxNotFoundRequests: 3);

        for (int i = 0; i < 3; i++)
        {
            await sut.RegisterNotFoundAsync(ClientKey, TestContext.Current.CancellationToken);
        }

        bool exceeded = await sut.IsExceededAsync(ClientKey, TestContext.Current.CancellationToken);

        exceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task IsExceededAsync_Should_Return_False_When_At_Limit()
    {
        IDistributedCache cache = CreateCache();
        NotFoundRateLimitTracker sut = CreateSut(cache, maxNotFoundRequests: 3);

        for (int i = 0; i < 3; i++)
        {
            await sut.RegisterNotFoundAsync(ClientKey, TestContext.Current.CancellationToken);
        }

        bool exceeded = await sut.IsExceededAsync(ClientKey, TestContext.Current.CancellationToken);

        exceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task IsExceededAsync_Should_Return_True_When_Above_Limit()
    {
        IDistributedCache cache = CreateCache();
        NotFoundRateLimitTracker sut = CreateSut(cache, maxNotFoundRequests: 3);

        for (int i = 0; i < 4; i++)
        {
            await sut.RegisterNotFoundAsync(ClientKey, TestContext.Current.CancellationToken);
        }

        bool exceeded = await sut.IsExceededAsync(ClientKey, TestContext.Current.CancellationToken);

        exceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task IsExceededAsync_Should_Ignore_Entries_Outside_Window()
    {
        IDistributedCache cache = CreateCache();
        TimeSpan window = TimeSpan.FromMinutes(1);
        NotFoundRateLimitTracker sut = CreateSut(cache, maxNotFoundRequests: 2, window: window);

        DateTime now = DateTime.UtcNow;
        List<DateTime> seeded =
        [
            now.AddMinutes(-5),
            now.AddMinutes(-4),
            now.AddMinutes(-3),
            now.AddMinutes(-2),
            now.AddSeconds(-30),
        ];
        await cache.SetAsync(
            CacheKey,
            JsonSerializer.SerializeToUtf8Bytes(seeded),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) },
            TestContext.Current.CancellationToken);

        bool exceeded = await sut.IsExceededAsync(ClientKey, TestContext.Current.CancellationToken);

        exceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task IsExceededAsync_Should_Count_Only_Entries_Within_Window()
    {
        IDistributedCache cache = CreateCache();
        TimeSpan window = TimeSpan.FromMinutes(1);
        NotFoundRateLimitTracker sut = CreateSut(cache, maxNotFoundRequests: 2, window: window);

        DateTime now = DateTime.UtcNow;
        List<DateTime> seeded =
        [
            now.AddMinutes(-5),
            now.AddSeconds(-30),
            now.AddSeconds(-20),
            now.AddSeconds(-10),
        ];
        await cache.SetAsync(
            CacheKey,
            JsonSerializer.SerializeToUtf8Bytes(seeded),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) },
            TestContext.Current.CancellationToken);

        bool exceeded = await sut.IsExceededAsync(ClientKey, TestContext.Current.CancellationToken);

        exceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task RegisterNotFoundAsync_Should_Append_Timestamp()
    {
        IDistributedCache cache = CreateCache();
        NotFoundRateLimitTracker sut = CreateSut(cache);

        DateTime before = DateTime.UtcNow;
        await sut.RegisterNotFoundAsync(ClientKey, TestContext.Current.CancellationToken);
        DateTime after = DateTime.UtcNow;

        List<DateTime> stored = await ReadCache(cache);
        stored.Count.ShouldBe(1);
        stored[0].ShouldBeInRange(before, after);
    }

    [Fact]
    public async Task RegisterNotFoundAsync_Should_Append_To_Existing_Timestamps()
    {
        IDistributedCache cache = CreateCache();
        NotFoundRateLimitTracker sut = CreateSut(cache);

        await sut.RegisterNotFoundAsync(ClientKey, TestContext.Current.CancellationToken);
        await sut.RegisterNotFoundAsync(ClientKey, TestContext.Current.CancellationToken);
        await sut.RegisterNotFoundAsync(ClientKey, TestContext.Current.CancellationToken);

        List<DateTime> stored = await ReadCache(cache);
        stored.Count.ShouldBe(3);
    }

    [Fact]
    public async Task RegisterNotFoundAsync_Should_Prune_Entries_Outside_Window()
    {
        IDistributedCache cache = CreateCache();
        TimeSpan window = TimeSpan.FromMinutes(1);
        NotFoundRateLimitTracker sut = CreateSut(cache, window: window);

        DateTime now = DateTime.UtcNow;
        List<DateTime> seeded =
        [
            now.AddMinutes(-5),
            now.AddMinutes(-2),
            now.AddSeconds(-30),
        ];
        await cache.SetAsync(
            CacheKey,
            JsonSerializer.SerializeToUtf8Bytes(seeded),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) },
            TestContext.Current.CancellationToken);

        await sut.RegisterNotFoundAsync(ClientKey, TestContext.Current.CancellationToken);

        List<DateTime> stored = await ReadCache(cache);
        stored.Count.ShouldBe(2);
        stored.ShouldNotContain(seeded[0]);
        stored.ShouldNotContain(seeded[1]);
        stored.ShouldContain(seeded[2]);
    }

    [Fact]
    public async Task Tracker_Should_Isolate_Different_Client_Keys()
    {
        IDistributedCache cache = CreateCache();
        NotFoundRateLimitTracker sut = CreateSut(cache, maxNotFoundRequests: 1);

        await sut.RegisterNotFoundAsync("client-a", TestContext.Current.CancellationToken);
        await sut.RegisterNotFoundAsync("client-a", TestContext.Current.CancellationToken);
        await sut.RegisterNotFoundAsync("client-a", TestContext.Current.CancellationToken);

        bool aExceeded = await sut.IsExceededAsync("client-a", TestContext.Current.CancellationToken);
        bool bExceeded = await sut.IsExceededAsync("client-b", TestContext.Current.CancellationToken);

        aExceeded.ShouldBeTrue();
        bExceeded.ShouldBeFalse();
    }

    private static async Task<List<DateTime>> ReadCache(IDistributedCache cache)
    {
        byte[]? bytes = await cache.GetAsync(CacheKey, TestContext.Current.CancellationToken);
        bytes.ShouldNotBeNull();
        List<DateTime>? timestamps = JsonSerializer.Deserialize<List<DateTime>>(bytes);
        timestamps.ShouldNotBeNull();
        return timestamps;
    }

    private static IDistributedCache CreateCache() =>
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    private static NotFoundRateLimitTracker CreateSut(
        IDistributedCache cache,
        int maxNotFoundRequests = 20,
        TimeSpan? window = null) =>
        new(
            cache,
            Options.Create(
                new NotFoundRateLimitOptions
                {
                    MaxNotFoundRequests = maxNotFoundRequests,
                    Window = window ?? TimeSpan.FromMinutes(1),
                }));
}
