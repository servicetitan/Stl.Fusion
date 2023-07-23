using Stl.Fusion.Client.Caching;
using Stl.Fusion.Client.Interception;
using Stl.Fusion.Tests.Services;
using Stl.Fusion.Tests.UIModels;

namespace Stl.Fusion.Tests;

public class KeyValueServiceWithCacheTest : FusionTestBase
{
    public KeyValueServiceWithCacheTest(ITestOutputHelper @out) : base(@out)
        => UseClientComputedCache = true;

    [Fact]
    public async Task BasicTest()
    {
        await using var serving = await WebHost.Serve();
        await Delay(0.25);
        var cache = ClientServices.GetRequiredService<IClientComputedCache>();
        await cache.WhenInitialized;

        var clientServices2 = CreateServices(true);
        await using var _ = clientServices2 as IAsyncDisposable;

        var kv = WebServices.GetRequiredService<IKeyValueService<string>>();
        var kv1 = ClientServices.GetRequiredService<IKeyValueService<string>>();
        var kv2 = clientServices2.GetRequiredService<IKeyValueService<string>>();
        var smallTimeout = TimeSpan.FromSeconds(1);
        var timeout = TimeSpan.FromSeconds(1);

        await kv.Set("1", "a");

        var c1 = await GetComputed(kv1, "1");
        c1.Value.Should().Be("a");
        c1.Call.Should().NotBeNull(); // Not from cache
        c1.CacheEntry.Should().BeNull(); // Not from cache

        await Assert.ThrowsAnyAsync<TimeoutException>(() =>
            c1.WhenInvalidated().WaitAsync(smallTimeout));

        var c2 = await GetComputed(kv2, "1");
        c2.Value.Should().Be("a");
        c2.Call.Should().BeNull(); // From cache
        c2.CacheEntry.Should().NotBeNull(); // From cache
        await c2.WhenCallCompleted();
        c2.Call.Should().NotBeNull();

        await kv.Set("1", "a");
        await c1.WhenInvalidated().WaitAsync(timeout);
        await c2.WhenInvalidated().WaitAsync(timeout);
    }

    private static async Task<ClientComputed<string>> GetComputed(IKeyValueService<string> kv, string key)
    {
        var c = await Computed.Capture(() => kv.Get(key));
        return (ClientComputed<string>)c;
    }
}
