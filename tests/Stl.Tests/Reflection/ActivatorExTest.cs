using Stl.Reflection;

namespace Stl.Tests.Reflection;

public class ActivatorExTest : TestBase
{
    public class SimpleClass { }

    public ActivatorExTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public void NewTest()
    {
        ActivatorExt.New<int>().Should().Be(0);
        ActivatorExt.New<int>(false).Should().Be(0);
        ActivatorExt.New<Unit>().Should().Be(default(Unit));
        ActivatorExt.New<Unit>(false).Should().Be(default(Unit));
        ActivatorExt.New<SimpleClass>().Should().BeOfType(typeof(SimpleClass));
        ActivatorExt.New<SimpleClass>(false).Should().BeOfType(typeof(SimpleClass));

        ((Func<string>) (() => ActivatorExt.New<string>()))
            .Should().Throw<InvalidOperationException>();
        ActivatorExt.New<string>(false).Should().Be(null);
    }
}
