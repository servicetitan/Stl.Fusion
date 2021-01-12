using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Tests.Services;
using Stl.OS;
using Stl.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class ScreenshotServiceClientTest : FusionTestBase
    {
        public ScreenshotServiceClientTest(ITestOutputHelper @out, FusionTestOptions? options = null) : base(@out, options) { }

        [Fact]
        public async Task BasicTest()
        {
            if (OSInfo.IsAnyUnix)
                // Screenshots don't work on Unix
                return;

            var epsilon = TimeSpan.FromSeconds(0.5);

            await using var serving = await WebSocketHost.ServeAsync();
            var service = ClientServices.GetRequiredService<IScreenshotServiceClient>();

            ScreenshotController.CallCount = 0;
            for (int i = 0; i < 20; i++) {
                var screenshot = await service.GetScreenshotAsync(100);
                (DateTime.Now - screenshot.CapturedAt).Should().BeLessThan(epsilon);
                await Task.Delay(TimeSpan.FromSeconds(0.1));
            }
            ScreenshotController.CallCount.Should().BeLessThan(10);
        }
    }
}
