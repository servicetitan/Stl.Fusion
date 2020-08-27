using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.Fusion.Tests.Services;
using Stl.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class ComputedInterceptorTest : FusionTestBase
    {
        public ComputedInterceptorTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task AutoRecomputeTest()
        {
            var time = Services.GetRequiredService<ITimeService>();
            var c = await Computed.CaptureAsync(
                _ => time.GetTimeWithOffsetAsync(TimeSpan.FromSeconds(1)));

            var count = 0L;
            using var state = StateFactory.NewLive<DateTime>(
                o => o.NoUpdateDelay(),
                async (_, ct) => await c.UseAsync(ct));
            state.Updated += s
                => Log.LogInformation($"{++count} -> {s.Value:hh:mm:ss:fff}");

            await Task.Delay(3000);
            state.Dispose();
            var lastCount = count;
            lastCount.Should().BeGreaterThan(3);

            await Task.Delay(1000);
            count.Should().Be(lastCount);
        }

        [Fact]
        public async Task InvalidationAndCachingTest1()
        {
            var time = Services.GetRequiredService<ITimeService>();

            var c1 = await Computed.CaptureAsync(_ => time.GetTimeAsync());

            // Wait for time invalidation
            await Task.Delay(500);

            var c2a = await Computed.CaptureAsync(_ => time.GetTimeAsync());
            c2a.Should().NotBeSameAs(c1);
            var c2b = await Computed.CaptureAsync(_ => time.GetTimeAsync());
            c2b.Should().BeSameAs(c2a);
        }

        [Fact]
        public async Task InvalidationAndCachingTest2()
        {
            // TODO: Fix the test so that it starts right after the time invalidation,
            // otherwise it has a tiny chance of failure
            var time = Services.GetRequiredService<ITimeService>();

            var c1 = await Computed.CaptureAsync(_ => time.GetTimeWithOffsetAsync(TimeSpan.FromSeconds(1)));
            c1.Should().NotBeNull();
            var c2 = await Computed.CaptureAsync(_ => time.GetTimeWithOffsetAsync(TimeSpan.FromSeconds(2)));
            c2.Should().NotBeNull();
            c1.Should().NotBeSameAs(c2);

            var c1a = await Computed.CaptureAsync(_ => time.GetTimeWithOffsetAsync(TimeSpan.FromSeconds(1)));
            var c2a = await Computed.CaptureAsync(_ => time.GetTimeWithOffsetAsync(TimeSpan.FromSeconds(2)));
            c1.Should().BeSameAs(c1a);
            c2.Should().BeSameAs(c2a);

            // Wait for time invalidation
            await Task.Delay(500);

            c1a = await Computed.CaptureAsync(_ => time.GetTimeWithOffsetAsync(TimeSpan.FromSeconds(1)));
            c2a = await Computed.CaptureAsync(_ => time.GetTimeWithOffsetAsync(TimeSpan.FromSeconds(2)));
            c1.Should().NotBeSameAs(c1a);
            c2.Should().NotBeSameAs(c2a);
        }
    }
}
