using Stl.Fusion.Tests.Services;
using Stl.OS;

namespace Stl.Fusion.Tests;

public class ScreenshotServiceClientTest : FusionTestBase
{
    public ScreenshotServiceClientTest(ITestOutputHelper @out, FusionTestOptions? options = null) : base(@out, options) { }

#if NETCOREAPP
    [Fact]
    public async Task BasicTest()
    {
        if (OSInfo.IsAnyUnix)
            // Screenshots don't work on Unix
            return;

        var epsilon = TimeSpan.FromSeconds(0.5);

        await using var serving = await WebHost.Serve();
        await Delay(0.25);
        var service = ClientServices.GetRequiredService<IScreenshotServiceClient>();

        ScreenshotController.CallCount = 0;
        for (var i = 0; i < 50; i++) {
            var screenshot = await service.GetScreenshot(100);
            screenshot.Should().NotBeNull();
            (SystemClock.Now - screenshot.CapturedAt).Should().BeLessThan(epsilon);
            await Task.Delay(TimeSpan.FromSeconds(0.02));
        }
        ScreenshotController.CallCount.Should().BeLessThan(15);
    }
#endif
}
