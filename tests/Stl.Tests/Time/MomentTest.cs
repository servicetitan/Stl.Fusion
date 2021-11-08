using Stl.Testing.Collections;

namespace Stl.Tests.Time;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class MomentTest : TestBase
{
    public MomentTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public void BasicTest()
    {
        var m = SystemClock.Now;
        var m1 = (Moment) m.ToDateTimeOffset();
        m1.Should().Equals(m);
        m1 = m.ToDateTime();
        m1.Should().Equals(m);

        m1 = m.PassThroughAllSerializers(Out);
        m1.Should().Equals(m);
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

    [Fact]
    public void ClampTest()
    {
        var m = new Moment(DateTime.Now);
        m.Clamp(m, m).Should().Be(m);
        m.Clamp(m + TimeSpan.FromSeconds(1), DateTime.MaxValue)
            .Should().Be(m + TimeSpan.FromSeconds(1));
        m.Clamp(DateTime.MinValue, m - TimeSpan.FromSeconds(1))
            .Should().Be(m - TimeSpan.FromSeconds(1));

        m = Moment.MinValue;
        m.ToDateTimeClamped().Should().Be(DateTime.MinValue.ToUniversalTime());
        m.ToDateTimeOffsetClamped().Should().Be(DateTimeOffset.MinValue.ToUniversalTime());
        m.ToString().Should().Be(new Moment(DateTime.MinValue).ToString());
        m = Moment.MaxValue;
        m.ToDateTimeClamped().Should().Be(DateTime.MaxValue.ToUniversalTime());
        m.ToDateTimeOffsetClamped().Should().Be(DateTimeOffset.MaxValue.ToUniversalTime());
        m.ToString().Should().Be(new Moment(DateTimeOffset.MaxValue).ToString());
    }
}
