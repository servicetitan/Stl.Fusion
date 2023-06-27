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
        var cache = ClientServices.GetRequiredService<ClientComputedCache>();
        await cache.WhenInitialized;

        var clientServices2 = CreateServices(true);
        await using var _ = clientServices2 as IAsyncDisposable;

        var service1 = ClientServices.GetRequiredService<IScreenshotService>();
        var service2 = clientServices2.GetRequiredService<IScreenshotService>();

        var sw = Stopwatch.StartNew();
        var c1 = await GetScreenshotComputed(service1);
        Out.WriteLine($"Miss in: {sw.ElapsedMilliseconds}ms");
        c1.Call.Should().NotBeNull();
        c1.Options.ClientCacheMode.Should().Be(ClientCacheMode.Cache);
        c1.Call!.ResultTask.IsCompleted.Should().BeTrue(); // First cache miss should resolve via Rpc

        sw.Restart();
        var c2 = await GetScreenshotComputed(service2);
        Out.WriteLine($"Hit in: {sw.ElapsedMilliseconds}ms");
        c2.Call.Should().BeNull(); // First cache hit should resolve w/o waiting for Rpc

        sw.Restart();
        await c2.WhenInvalidated().WaitAsync(TimeSpan.FromSeconds(5));
        Out.WriteLine($"Invalidated in: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        c2 = (ClientComputed<Screenshot>)await c2.Update().ConfigureAwait(false);
        Out.WriteLine($"Updated in: {sw.ElapsedMilliseconds}ms");
        c2.Call!.ResultTask.IsCompleted.Should().BeTrue(); // Should resolve via Rpc at this point
    }

    [Fact]
    public async Task GetScreenshotAltTest()
    {
        if (OSInfo.IsAnyUnix)
            // Screenshots don't work on Unix
            return;

        await using var serving = await WebHost.Serve();
        await Delay(0.25);
        var cache = ClientServices.GetRequiredService<ClientComputedCache>();
        await cache.WhenInitialized;

        var clientServices2 = CreateServices(true);
        await using var _ = clientServices2 as IAsyncDisposable;

        var service1 = ClientServices.GetRequiredService<IScreenshotService>();
        var service2 = clientServices2.GetRequiredService<IScreenshotService>();

        var sw = Stopwatch.StartNew();
        var c1 = await GetScreenshotAltComputed(service1);
        Out.WriteLine($"Miss in: {sw.ElapsedMilliseconds}ms");
        c1.Options.ClientCacheMode.Should().Be(ClientCacheMode.NoCache);
        c1.Call!.ResultTask.IsCompleted.Should().BeTrue(); // First cache miss should resolve via Rpc
        c1.Output.Value.Should().NotBeNull();

        sw.Restart();
        await c1.WhenInvalidated().WaitAsync(TimeSpan.FromSeconds(1));
        Out.WriteLine($"Invalidated in: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        var c2 = await GetScreenshotAltComputed(service2);
        Out.WriteLine($"2nd miss in: {sw.ElapsedMilliseconds}ms");
        c2.Call!.ResultTask.IsCompleted.Should().BeTrue();
        c1.Output.Value.Should().NotBeNull();

        sw.Restart();
        await c2.WhenInvalidated().WaitAsync(TimeSpan.FromSeconds(1));
        Out.WriteLine($"Invalidated in: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        c2 = (ClientComputed<Screenshot>)await c2.Update().ConfigureAwait(false);
        Out.WriteLine($"Updated in: {sw.ElapsedMilliseconds}ms");
        c2.Call!.ResultTask.IsCompleted.Should().BeTrue(); // Should resolve via Rpc at this point
        c2.Output.Value.Should().NotBeNull();
    }

    private static async Task<ClientComputed<Screenshot>> GetScreenshotComputed(IScreenshotService service)
        => (ClientComputed<Screenshot>)await Computed.Capture(() => service.GetScreenshot(100));

    private static async Task<ClientComputed<Screenshot>> GetScreenshotAltComputed(IScreenshotService service)
        => (ClientComputed<Screenshot>)await Computed.Capture(() => service.GetScreenshotAlt(100));
}
