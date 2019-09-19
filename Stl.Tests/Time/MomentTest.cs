using System;
using FluentAssertions;
using Stl.Testing;
using Stl.Time;
using Stl.Time.Clocks;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Time
{
    public class MomentTest : TestBase
    {
        public MomentTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void BasicTest()
        {
            var m = RealTimeClock.Now;
            var m1 = (Moment) m.ToDateTimeOffset();
            m1.Should().Equals(m);
            m1 = (Moment) m.ToDateTime();
            m1.Should().Equals(m);

            var e = Event.New("Test", m);
            var (e1, json) = e.PassThroughAllSerializersWithOutput();
            Out.WriteLine(e.ToString());
            Out.WriteLine(json);
            e1.Should().Equals(e);
        }

        [Fact]
        public void UtcHandlingTest()
        {
            var epsilon = TimeSpan.FromSeconds(1);
            
            var now1 = (Moment) DateTime.UtcNow;
            var now2 = (Moment) DateTime.Now;
            Math.Abs((now1 - now2).Ticks).Should().BeLessThan(epsilon.Ticks);
            
            now1 = DateTimeOffset.UtcNow;
            now2 = DateTimeOffset.Now;
            Math.Abs((now1 - now2).Ticks).Should().BeLessThan(epsilon.Ticks);
        }
    }
}
