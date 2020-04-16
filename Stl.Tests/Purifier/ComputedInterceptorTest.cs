using System;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
            var timeProvider = Container.Resolve<ITimeProvider>();
            var cTimer = await timeProvider.GetTimerAsync(TimeSpan.Zero);

            var count = 0;
            void Handler(IComputed<Moment> computed, Result<Moment> old, object? invalidatedBy)
            {
                Out.WriteLine($"{++count} -> {computed.Value}");
            }

            using (var o = cTimer.AutoRecompute(Handler)) {
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
            var timeProvider = Container.Resolve<ITimeProvider>();
            var cNowOld = timeProvider.GetTimeAsync();
            await Task.Delay(500);
            var cNow1 = await timeProvider.GetTimeAsync();
            cNow1.Should().NotBe(cNowOld);
            var cNow2 = await timeProvider.GetTimeAsync();
            cNow2.Should().Be(cNow1);
        }

        [Fact]
        public async Task CachingTest2()
        {
            var timeProvider = Container.Resolve<ITimeProvider>();
            var cTimer1 = timeProvider.GetTimerAsync(TimeSpan.FromSeconds(1));
            var cTimer2 = timeProvider.GetTimerAsync(TimeSpan.FromSeconds(2));
            cTimer1.Should().NotBe(cTimer2);
            var cTimer1a = timeProvider.GetTimerAsync(TimeSpan.FromSeconds(1));
            var cTimer2a = timeProvider.GetTimerAsync(TimeSpan.FromSeconds(2));
            cTimer1.Should().Be(cTimer1a);
            cTimer2.Should().Be(cTimer2a);
            await Task.Delay(500);
            cTimer1a = timeProvider.GetTimerAsync(TimeSpan.FromSeconds(1));
            cTimer2a = timeProvider.GetTimerAsync(TimeSpan.FromSeconds(2));
            cTimer1.Should().NotBe(cTimer1a);
            cTimer2.Should().NotBe(cTimer2a);
        }
    }
}
