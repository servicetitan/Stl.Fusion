using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Extensions;
using Stl.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests.Extensions
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class LiveTimeTest : FusionTestBase
    {
        public LiveTimeTest(ITestOutputHelper @out)
            : base(@out, new FusionTestOptions() { UseTestClock = true })
        { }

        [Fact]
        public async Task BasicTest()
        {
            var liveClock = Services.GetRequiredService<ILiveClock>();

            var cTime = await Computed.CaptureAsync(_ => liveClock.GetUtcNowAsync());
            cTime.IsConsistent().Should().BeTrue();
            (DateTime.UtcNow - cTime.Value).Should().BeLessThan(TimeSpan.FromSeconds(1.1));
            await DelayAsync(1.3);
            cTime.IsConsistent().Should().BeFalse();

            cTime = await Computed.CaptureAsync(_ => liveClock.GetUtcNowAsync(TimeSpan.FromMilliseconds(200)));
            cTime.IsConsistent().Should().BeTrue();
            await DelayAsync(0.25);
            cTime.IsConsistent().Should().BeFalse();

            var now = DateTime.UtcNow;
            var ago = await liveClock.GetMomentsAgoAsync(now);
            ago.Should().Be("just now");
            await DelayAsync(1.8);
            ago = await liveClock.GetMomentsAgoAsync(now);
            ago.Should().Be("1 second ago");
        }
    }
}
