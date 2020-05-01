using System;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Stl.Fusion;
using Stl.Tests.Fusion.Services;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Fusion
{
    public class ComputedInterceptorTest : PurifierTestBase, IAsyncLifetime
    {
        public ComputedInterceptorTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task AutoRecomputeTest()
        {
            var time = Container.Resolve<ITimeProvider>();
            var c = await Computed.CaptureAsync(
                () => time.GetTimeOffsetAsync(TimeSpan.FromSeconds(1)));

            var count = 0;
            void OnInvalidated(IComputed<DateTime> @new, Result<DateTime> old, object? invalidatedBy) 
                => Log.LogInformation($"{++count} -> {@new.Value:hh:mm:ss:fff}");

            using (var _ = c!.AutoRenew(OnInvalidated)) {
                await Task.Delay(2000);
            }
            var lastCount = count;
            Out.WriteLine("Completed AutoRecompute.");

            await Task.Delay(1000);
            count.Should().Be(lastCount);
            count.Should().BeGreaterThan(4);
        }

        [Fact]
        public async Task InvalidationAndCachingTest1()
        {
            var time = Container.Resolve<ITimeProvider>();

            var c1 = await Computed.CaptureAsync(() => time.GetTimeAsync());
            
            // Wait for time invalidation
            await Task.Delay(500);
            
            var c2a = await Computed.CaptureAsync(() => time.GetTimeAsync());
            c2a.Should().NotBeSameAs(c1);
            var c2b = await Computed.CaptureAsync(() => time.GetTimeAsync());
            c2b.Should().BeSameAs(c2a);
        }

        [Fact]
        public async Task InvalidationAndCachingTest2()
        {
            // TODO: Fix the test so that it starts right after the time invalidation,
            // otherwise it has a tiny chance of failure
            var time = Container.Resolve<ITimeProvider>();

            var c1 = await Computed.CaptureAsync(() => time.GetTimeOffsetAsync(TimeSpan.FromSeconds(1)));
            c1.Should().NotBeNull();
            var c2 = await Computed.CaptureAsync(() => time.GetTimeOffsetAsync(TimeSpan.FromSeconds(2)));
            c2.Should().NotBeNull();
            c1.Should().NotBeSameAs(c2);

            var c1a = await Computed.CaptureAsync(() => time.GetTimeOffsetAsync(TimeSpan.FromSeconds(1)));
            var c2a = await Computed.CaptureAsync(() => time.GetTimeOffsetAsync(TimeSpan.FromSeconds(2)));
            c1.Should().BeSameAs(c1a);
            c2.Should().BeSameAs(c2a);

            // Wait for time invalidation
            await Task.Delay(500);

            c1a = await Computed.CaptureAsync(() => time.GetTimeOffsetAsync(TimeSpan.FromSeconds(1)));
            c2a = await Computed.CaptureAsync(() => time.GetTimeOffsetAsync(TimeSpan.FromSeconds(2)));
            c1.Should().NotBeSameAs(c1a);
            c2.Should().NotBeSameAs(c2a);
        }
    }
}
