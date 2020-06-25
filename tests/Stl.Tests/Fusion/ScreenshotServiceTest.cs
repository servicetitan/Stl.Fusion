using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
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
            var screenshots = Container.Resolve<IScreenshotService>();
            var s1 = await screenshots.GetScreenshotAsync(128);
            s1.Length.Should().BeGreaterThan(0);
        }
    }
}
