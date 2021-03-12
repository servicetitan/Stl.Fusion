using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Tests.Services;
using Stl.OS;
using Stl.Testing;
using Stl.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
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
                c.Value.Base64Content.Length.Should().BeGreaterThan(0);
                await TestEx.WhenMet(
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
}
