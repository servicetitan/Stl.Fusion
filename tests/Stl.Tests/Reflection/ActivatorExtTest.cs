using Stl.Reflection;

namespace Stl.Tests.Reflection;

public class ActivatorExtTest : TestBase
{
    public class SimpleClass { }

    public ActivatorExtTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public void CreateInstanceTest()
    {
        typeof(R0).CreateInstance()
            .Should().BeOfType<R0>();
        typeof(R1).CreateInstance(1)
            .Should().BeOfType<R1>();
        typeof(R2).CreateInstance(1, false)
            .Should().BeOfType<R2>();
        typeof(R3).CreateInstance(1, false, default(Unit))
            .Should().BeOfType<R3>();
        typeof(R4).CreateInstance(1, false, default(Unit), "1")
            .Should().BeOfType<R4>();
        typeof(R5).CreateInstance(1, false, default(Unit), "1", 1.0d)
            .Should().BeOfType<R5>();
    }

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

    // Nested types

    public record R0();
    public record R1(int A);
    public record R2(int A, bool B);
    public record R3(int A, bool B, Unit C);
    public record R4(int A, bool B, Unit C, string D);
    public record R5(int A, bool B, Unit C, string D, double E);
}
