using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion;
using Stl.OS;
using Stl.Tests.Fusion.Services;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Fusion
{
    public class ScreenshotServiceTest : FusionTestBase, IAsyncLifetime
    {
        public ScreenshotServiceTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task BasicTest()
        {
            if (OSInfo.Kind == OSKind.Unix)
                // Screenshots don't work on Unix
                return;

            var screenshots = Services.GetRequiredService<IScreenshotService>();
            var c = await Computed.CaptureAsync(_ => screenshots.GetScreenshotAsync(128));
            c.IsConsistent.Should().BeTrue();
            c.Value.Length.Should().BeGreaterThan(0);
            await Task.Delay(200);
            c.IsConsistent.Should().BeFalse();
        }
    }
}
