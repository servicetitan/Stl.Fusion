using System;
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
            baseTypes.Should().BeSameAs(new[] {typeof(TestBase), typeof(object)});
        }

        [Fact]
        public void ToMethodNameTest()
        {
            typeof(Box).ToMethodName().Should().Equals("Box");
            typeof(Box).ToMethodName(true).Should().Equals("Stl_Internal_Box");

            typeof(Box<>).ToMethodName().Should().Equals("Box_1");
            typeof(Box<>).ToMethodName(true).Should().Equals("Stl_Internal_Box_1");

            typeof(Box<object>).ToMethodName().Should().Equals("Box_Object");
            typeof(Box<object>).ToMethodName(true).Should().Equals("Stl_Internal_Box_Object");
            typeof(Box<object>).ToMethodName(true, true).Should().Equals("Stl_Internal_Box_System_Object");

            typeof(Dictionary<,>).ToMethodName().Should().Equals("Dictionary_2");
            typeof(Dictionary<,>).ToMethodName(true).Should().Equals("System_Collections_Generic_Dictionary_2");
            typeof(Dictionary<int, byte>).ToMethodName().Should().Equals("Dictionary_Int32_Byte");

            typeof(Box<Box<int>>).ToMethodName().Should().Equals("Box_Int_Int");
            typeof(Box<Box<int>>).ToMethodName(true).Should().Equals("Stl_Internal_Box_Int_Int");
            typeof(Box<Box<int>>).ToMethodName(false, true).Should().Equals("Box_System_Int_System_Int");
            typeof(Box<Box<int>>).ToMethodName(true, true).Should().Equals("Stl_Internal_Box_System_Int_System_Int");
        }
    }
}
