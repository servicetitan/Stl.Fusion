using Stl.Reflection;

namespace Stl.Tests.Reflection;

public class TypeRefTest : TestBase
{
    public TypeRefTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public void BasicTest()
    {
        var r = (TypeRef) typeof(TypeRefTest);
        r.Resolve().Should().Be(typeof(TypeRefTest));

        r = typeof(bool);
        r.Resolve().Should().Be(typeof(bool));

        r = typeof(Nested);
        r.Resolve().Should().Be(typeof(Nested));

        r = typeof(Nested.SubNested);
        r.Resolve().Should().Be(typeof(Nested.SubNested));

        r = typeof(PrivateNested);
        r.Resolve().Should().Be(typeof(PrivateNested));

        r = typeof(InternalNested);
        r.Resolve().Should().Be(typeof(InternalNested));

        r = typeof(ProtectedNested);
        r.Resolve().Should().Be(typeof(ProtectedNested));

        r = new TypeRef("NoSuchAssembly.NoSuchType");
        r.TryResolve().Should().BeNull();
        Assert.ThrowsAny<KeyNotFoundException>(() => r.Resolve());

        r = typeof(StaticType);
        r.Resolve().Should().Be(typeof(StaticType));

        r = typeof(StaticType.Nested);
        r.Resolve().Should().Be(typeof(StaticType.Nested));
    }

    [Fact]
    public void TrimAssemblyVersionTest()
    {
        var r = (TypeRef) typeof(TypeRefTest);
        var r1 = r.TrimAssemblyVersion();
        r1.AssemblyQualifiedName.Should().Be("Stl.Tests.Reflection.TypeRefTest, Stl.Tests");
        r1.Resolve().Should().Be(typeof(TypeRefTest));
    }

#pragma warning disable RCS1102
    public class Nested
    {
        public class SubNested { }
    }

    private class PrivateNested { }
    internal class InternalNested { }
    protected class ProtectedNested { }
#pragma warning restore RCS1102
}

public static class StaticType
{
    public class Nested { }
}
