using FluentAssertions;
using Stl.Reflection;
using Xunit;

namespace Stl.Tests.Reflection
{
    public class MemberwiseCopierTest
    {
        private readonly MemberwiseClonerTest _memberwiseClonerTest = new MemberwiseClonerTest();

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
            MemberwiseCopier.CopyMembers(e, c, o => o.AddFields());
            c.Property.Should().Be(e.Property);
            c.Field.Should().Be(e.Field);
        }
    }
}
