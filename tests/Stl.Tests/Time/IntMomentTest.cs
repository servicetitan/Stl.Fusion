using System;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.Testing;
using Stl.Time;
using Stl.Time.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Time
{
    public class IntMomentTest : TestBase
    {
        public IntMomentTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task BasicTest()
        {
            var m = IntMoment.Now;
            var m1 = (IntMoment) m.ToDateTimeOffset();
            m1.Should().Equals(m);
            m1 = m.ToDateTime();
            m1.Should().Equals(m);

            await Task.Delay(1000);
            m1 = IntMoment.Now;
            (m1 - m + 2).Should().BeGreaterThan(IntMoment.UnitsPerSecond);
        }

        [Fact]
        public void UtcHandlingTest()
        {
            var epsilon = (int) (TimeSpan.FromSeconds(1).Ticks / IntMoment.TicksPerUnit);
            
            var now1 = (IntMoment) DateTime.UtcNow;
            var now2 = (IntMoment) DateTime.Now;
            Math.Abs(now1 - now2).Should().BeLessThan(epsilon);
            
            now1 = DateTimeOffset.UtcNow;
            now2 = DateTimeOffset.Now;
            Math.Abs(now1 - now2).Should().BeLessThan(epsilon);
        }
    }
}
