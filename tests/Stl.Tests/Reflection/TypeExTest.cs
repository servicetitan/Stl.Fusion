using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Stl.Internal;
using Stl.Reflection;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Reflection
{
    public class TypeExTest : TestBase
    {
        public TypeExTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void GetAllBaseTypesTest()
        {
            var baseTypes = GetType().GetAllBaseTypes().ToArray();
            baseTypes.Should().BeEquivalentTo(typeof(TestBase), typeof(object));
        }

        [Fact]
        public void ToIdentifierNameTest()
        {
            typeof(Box).ToIdentifierName().Should().Equals("Box");
            typeof(Box).ToIdentifierName(true).Should().Equals("Stl_Internal_Box");

            typeof(Box<>).ToIdentifierName().Should().Equals("Box_1");
            typeof(Box<>).ToIdentifierName(true).Should().Equals("Stl_Internal_Box_1");

            typeof(Box<object>).ToIdentifierName().Should().Equals("Box_Object");
            typeof(Box<object>).ToIdentifierName(true).Should().Equals("Stl_Internal_Box_Object");
            typeof(Box<object>).ToIdentifierName(true, true).Should().Equals("Stl_Internal_Box_System_Object");

            typeof(Dictionary<,>).ToIdentifierName().Should().Equals("Dictionary_2");
            typeof(Dictionary<,>).ToIdentifierName(true).Should().Equals("System_Collections_Generic_Dictionary_2");
            typeof(Dictionary<int, byte>).ToIdentifierName().Should().Equals("Dictionary_Int32_Byte");

            typeof(Box<Box<int>>).ToIdentifierName().Should().Equals("Box_Int_Int");
            typeof(Box<Box<int>>).ToIdentifierName(true).Should().Equals("Stl_Internal_Box_Int_Int");
            typeof(Box<Box<int>>).ToIdentifierName(false, true).Should().Equals("Box_System_Int_System_Int");
            typeof(Box<Box<int>>).ToIdentifierName(true, true).Should().Equals("Stl_Internal_Box_System_Int_System_Int");
        }
    }
}
