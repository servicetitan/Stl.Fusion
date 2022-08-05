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
    }

    public class Nested
    {
        public class SubNested { }
    }

    private class PrivateNested { }
    internal class InternalNested { }
    protected class ProtectedNested { }
}
