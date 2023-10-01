using Stl.Fusion.Client.Caching;
using Stl.Fusion.Client.Interception;
using Stl.Fusion.Tests.Services;
using Stl.OS;

namespace Stl.Fusion.Tests;

public class ScreenshotServiceClientWithCacheTest : FusionTestBase
{
    public ScreenshotServiceClientWithCacheTest(ITestOutputHelper @out) : base(@out)
        => UseClientComputedCache = true;

    [Fact]
    public async Task GetScreenshotTest()
    {
        if (OSInfo.IsAnyUnix)
            // Screenshots don't work on Unix
            return;

        await using var serving = await WebHost.Serve();
        await Delay(0.25);
        var cache = ClientServices.GetRequiredService<IClientComputedCache>();
        await cache.WhenInitialized;

        var clientServices2 = CreateServices(true);
        await using var _ = clientServices2 as IAsyncDisposable;

        var service1 = ClientServices.GetRequiredService<IScreenshotService>();
        var service2 = clientServices2.GetRequiredService<IScreenshotService>();
        var timeout = TimeSpan.FromSeconds(1);

        var sw = Stopwatch.StartNew();
        var c1 = await GetScreenshotComputed(service1);
        Out.WriteLine($"Miss in: {sw.ElapsedMilliseconds}ms");
        c1.WhenSynchronized().IsCompleted.Should().BeTrue();
        c1.Options.ClientCacheMode.Should().Be(ClientCacheMode.Cache);

        sw.Restart();
        var c2 = await GetScreenshotComputed(service2);
        Out.WriteLine($"Hit in: {sw.ElapsedMilliseconds}ms");
        var whenSynchronized = c2.WhenSynchronized();
        whenSynchronized.IsCompleted.Should().BeFalse();
        await whenSynchronized;
        c2 = await GetScreenshotComputed(service2);
        c2.WhenSynchronized().IsCompleted.Should().BeTrue();

        sw.Restart();
        await c2.WhenInvalidated().WaitAsync(timeout);
        Out.WriteLine($"Invalidated in: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        c2 = await GetScreenshotComputed(service2);
        Out.WriteLine($"Updated in: {sw.ElapsedMilliseconds}ms");
        c2.WhenSynchronized().IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetScreenshotAltTest()
    {
        if (OSInfo.IsAnyUnix)
            // Screenshots don't work on Unix
            return;

        await using var serving = await WebHost.Serve();
        await Delay(0.25);
        var cache = ClientServices.GetRequiredService<IClientComputedCache>();
        await cache.WhenInitialized;

        var clientServices2 = CreateServices(true);
        await using var _ = clientServices2 as IAsyncDisposable;

        var service1 = ClientServices.GetRequiredService<IScreenshotService>();
        var service2 = clientServices2.GetRequiredService<IScreenshotService>();
        var timeout = TimeSpan.FromSeconds(1);

        var sw = Stopwatch.StartNew();
        var c1 = await GetScreenshotAltComputed(service1);
        Out.WriteLine($"Miss in: {sw.ElapsedMilliseconds}ms");
        c1.Output.Value.Should().NotBeNull();
        c1.Options.ClientCacheMode.Should().Be(ClientCacheMode.NoCache);
        c1.WhenSynchronized().IsCompleted.Should().BeTrue();

        sw.Restart();
        await c1.WhenInvalidated().WaitAsync(timeout);
        Out.WriteLine($"Invalidated in: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        var c2 = await GetScreenshotAltComputed(service2);
        Out.WriteLine($"2nd miss in: {sw.ElapsedMilliseconds}ms");
        c2.Output.Value.Should().NotBeNull();
        c2.WhenSynchronized().IsCompleted.Should().BeTrue();

        sw.Restart();
        await c2.WhenInvalidated().WaitAsync(timeout);
        Out.WriteLine($"Invalidated in: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        c2 = (ClientComputed<Screenshot>)await c2.Update().ConfigureAwait(false);
        Out.WriteLine($"Updated in: {sw.ElapsedMilliseconds}ms");
        c2.Output.Value.Should().NotBeNull();
        c2.WhenSynchronized().IsCompleted.Should().BeTrue();
    }

    private static async Task<ClientComputed<Screenshot>> GetScreenshotComputed(IScreenshotService service)
        => (ClientComputed<Screenshot>)await Computed.Capture(() => service.GetScreenshot(100));

    private static async Task<ClientComputed<Screenshot>> GetScreenshotAltComputed(IScreenshotService service)
        => (ClientComputed<Screenshot>)await Computed.Capture(() => service.GetScreenshotAlt(100));
}
