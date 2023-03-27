using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Tests.Services;
using Stl.OS;

namespace Stl.Fusion.Tests;

public class ScreenshotServiceClientWithReplicaCacheTest : FusionTestBase
{
    public ScreenshotServiceClientWithReplicaCacheTest(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() { UseReplicaCache = true})
    { }

    [Fact]
    public async Task BasicTest()
    {
        if (OSInfo.IsAnyUnix)
            // Screenshots don't work on Unix
            return;

        await using var serving = await WebHost.Serve();
        await Delay(0.25);

        var clientServices2 = CreateServices(true);
        await using var _ = clientServices2 as IAsyncDisposable;

        var service1 = ClientServices.GetRequiredService<IScreenshotServiceClient>();
        var service2 = clientServices2.GetRequiredService<IScreenshotServiceClient>();

        var sw = Stopwatch.StartNew();
        var c1 = await GetScreenshotComputed(service1);
        c1.Replica.Should().NotBeNull(); // First cache miss should resolve via replica
        Out.WriteLine($"Miss in: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        var c2 = await GetScreenshotComputed(service2);
        c2.Replica.Should().BeNull(); // First cache hit should have no replica
        Out.WriteLine($"Hit in: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        await c2.WhenInvalidated().WaitAsync(TimeSpan.FromSeconds(1));
        Out.WriteLine($"Invalidated in: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        c2 = (IReplicaMethodComputed)await c2.Update().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
        Out.WriteLine($"Updated in: {sw.ElapsedMilliseconds}ms");
        c2.Replica.Should().NotBeNull();
    }

    private static async Task<IReplicaMethodComputed> GetScreenshotComputed(IScreenshotServiceClient service)
        => (IReplicaMethodComputed)await Computed.Capture(() => service.GetScreenshot(100));
}
