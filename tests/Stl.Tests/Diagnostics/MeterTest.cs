using Stl.Diagnostics;

namespace Stl.Tests.Diagnostics;

public class MeterTest : TestBase
{
    public MeterTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public void GetMeterTest()
    {
        var a = typeof(Result).GetMeter();
        Out.WriteLine(a.Name);
        Out.WriteLine(a.Version);
        a.Name.Should().Be("Stl");
        a.Version.Should().StartWith("2.");
        a.Version.Should().Contain("+");

        var b = typeof(Result).GetMeter();
        b.Should().BeSameAs(a);
    }
}
