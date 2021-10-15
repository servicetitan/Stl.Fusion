using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Tests.Services;
using Stl.OS;
using Stl.Testing.Collections;

namespace Stl.Fusion.Tests;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class ScreenshotServiceTest : FusionTestBase
{
    public ScreenshotServiceTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task BasicTest()
    {
        if (OSInfo.IsAnyUnix)
            // Screenshots don't work on Unix
            return;

        var c = await GetScreenshotComputed();
        for (var i = 0; i < 10; i++) {
            c.Value.Image.Data.Length.Should().BeGreaterThan(0);
            await TestExt.WhenMet(
                () => c.IsConsistent().Should().BeFalse(),
                TimeSpan.FromSeconds(0.5));
            c = await GetScreenshotComputed();
        }
    }

    private async Task<IComputed<Screenshot>> GetScreenshotComputed()
    {
        var screenshots = Services.GetRequiredService<IScreenshotService>();
        var computed = await Computed.Capture(_ => screenshots.GetScreenshot(1280));
        return computed;
    }
}
