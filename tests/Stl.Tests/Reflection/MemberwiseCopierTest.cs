using Stl.Reflection;

namespace Stl.Tests.Reflection;

public class MemberwiseCopierTest
{
    public class Example
    {
        public int Property { get; set; }
        public string Field = "";
    }

    [Fact]
    public void Test()
    {
        var e = new Example() { Property = 3, Field = "3" };
        var c = new Example();
        MemberwiseCopier.Invoke(e, c, copier => copier.WithFields());
        c.Property.Should().Be(e.Property);
        c.Field.Should().Be(e.Field);
    }
}
