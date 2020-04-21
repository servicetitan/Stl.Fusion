using System;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Stl.Purifier;
using Stl.Tests.Purifier.Services;
using Stl.Time;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Purifier
{
    public class ComputedInterceptorTest : PurifierTestBase, IAsyncLifetime
    {
        public ComputedInterceptorTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task AutoRecomputeTest()
        {
            var time = Container.Resolve<ITimeProvider>();
            var cTimer = await Computed.Capture(() => time.GetTimeOffsetAsync(TimeSpan.Zero));

            var count = 0;
            void Handler(IComputed<Moment> computed, Result<Moment> old, object? invalidatedBy)
            {
                Out.WriteLine($"{++count} -> {computed.Value}");
            }

            using (var o = cTimer!.AutoRecompute(Handler)) {
                await Task.Delay(2000);
            }
            var lastCount = count;
            Out.WriteLine("Disposed.");

            await Task.Delay(1000);
            count.Should().Be(lastCount);
            count.Should().BeGreaterThan(5);
        }

        [Fact]
        public async Task CachingTest1()
        {
            var time = Container.Resolve<ITimeProvider>();

            var cNowOld = time.GetTimeAsync();
            await Task.Delay(500);
            var cNow1 = await Computed.Capture(() => time.GetTimeAsync());
            cNow1.Should().NotBe(cNowOld);
            var cNow2 = await Computed.Capture(() => time.GetTimeAsync());
            cNow2.Should().Be(cNow1);
        }

        [Fact]
        public async Task CachingTest2()
        {
            // Need to fix the test so that it starts right after time invalidation
            var time = Container.Resolve<ITimeProvider>();

            var cTimer1 = await Computed.Capture(() => time.GetTimeOffsetAsync(TimeSpan.FromSeconds(1)));
            cTimer1.Should().NotBeNull();
            var cTimer2 = await Computed.Capture(() => time.GetTimeOffsetAsync(TimeSpan.FromSeconds(2)));
            cTimer2.Should().NotBeNull();
            cTimer1.Should().NotBeSameAs(cTimer2);

            var cTimer1a = await Computed.Capture(() => time.GetTimeOffsetAsync(TimeSpan.FromSeconds(1)));
            var cTimer2a = await Computed.Capture(() => time.GetTimeOffsetAsync(TimeSpan.FromSeconds(2)));
            cTimer1.Should().BeSameAs(cTimer1a);
            cTimer2.Should().BeSameAs(cTimer2a);
            await Task.Delay(500);

            cTimer1a = await Computed.Capture(() => time.GetTimeOffsetAsync(TimeSpan.FromSeconds(1)));
            cTimer2a = await Computed.Capture(() => time.GetTimeOffsetAsync(TimeSpan.FromSeconds(2)));
            cTimer1.Should().NotBeSameAs(cTimer1a);
            cTimer2.Should().NotBeSameAs(cTimer2a);
        }
    }
}
