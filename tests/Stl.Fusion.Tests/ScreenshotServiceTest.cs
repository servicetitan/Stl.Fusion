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
            if (OSInfo.Kind == OSKind.Unix)
                // Screenshots don't work on Unix
                return;

            var c = await GetScreenshotComputedAsync();
            for (var i = 0; i < 10; i++) {
                c.Value.Base64Content.Length.Should().BeGreaterThan(0);
                await TestEx.WhenMetAsync(
                    () => c.IsConsistent().Should().BeFalse(),
                    TimeSpan.FromSeconds(0.5));
                c = await GetScreenshotComputedAsync();
            }
        }

        private async Task<IComputed<Screenshot>> GetScreenshotComputedAsync()
        {
            var screenshots = Services.GetRequiredService<IScreenshotService>();
            var computed = await Computed.CaptureAsync(_ => screenshots.GetScreenshotAsync(1280));
            return computed;
        }
    }
}
