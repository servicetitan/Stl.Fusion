using Stl.Reflection;

namespace Stl.Tests.Reflection;

public class MemberwiseClonerTest
{
    [Fact]
    public void Test()
    {
        var i = 3;
        MemberwiseCloner.Invoke(i).Should().Be(i);

        var a = new [] {1, 2, 3};
        var b = MemberwiseCloner.Invoke(a);
        b.Should().NotBeSameAs(a);
        b.Should().Equal(a);
    }
}
