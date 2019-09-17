using System;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.Testing;
using Stl.Time;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Time
{
    public class ClockTest : TestBase
    {
        public ClockTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task BasicTest()
        {
            var epsilon = TimeSpan.FromSeconds(0.1);
            
            var zero = (Moment) DateTime.Now;
            Math.Abs((Moment.Now - zero).Ticks).Should().BeLessThan(epsilon.Ticks);

            Moment time;
            var fasterClock = Clock.Current.SpeedupBy(10);
            using (fasterClock.Activate(true)) {
                await Clock.Current.Delay(TimeSpan.FromSeconds(5));
                var fasterEpsilon = epsilon * 10;
                Math.Abs((Moment.Now - zero - TimeSpan.FromSeconds(5)).Ticks).Should().BeLessThan(fasterEpsilon.Ticks);
                Math.Abs((DateTime.Now.ToMoment() - zero - TimeSpan.FromSeconds(0.5)).Ticks).Should().BeLessThan(epsilon.Ticks);
                time = Moment.Now;
            }
            Math.Abs((Moment.Now - time).Ticks).Should().BeLessThan(epsilon.Ticks);

            zero = Moment.Now;
            await Clock.Current.Delay(TimeSpan.FromSeconds(1));
            Math.Abs((Moment.Now - zero - TimeSpan.FromSeconds(1)).Ticks).Should().BeLessThan(epsilon.Ticks);
            Math.Abs((DateTime.Now.ToMoment() - zero - TimeSpan.FromSeconds(1)).Ticks).Should().BeLessThan(epsilon.Ticks);
        }
    }
}