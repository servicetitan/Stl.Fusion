using Stl.Diagnostics;

namespace Stl.Tests.Diagnostics;

public class ActivitySourceTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public void GetActivitySourceTest()
    {
        var a = typeof(Result).GetActivitySource();
        Out.WriteLine(a.Name);
        Out.WriteLine(a.Version);
        a.Name.Should().Be("Stl");
        a.Version.Should().StartWith("6.");
        a.Version.Should().Contain("+");

        var b = typeof(Result).GetActivitySource();
        b.Should().BeSameAs(a);
    }
}
