using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Stl.Async;
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
            baseTypes.Should().BeEquivalentTo(new [] {typeof(TestBase), typeof(object)});

            baseTypes = typeof(AsyncProcessBase).GetAllBaseTypes(true, true).ToArray();
            var adwdsIndex = Array.IndexOf(baseTypes, typeof(IAsyncDisposableWithDisposalState));
            var apIndex = Array.IndexOf(baseTypes, typeof(IAsyncProcess));
            var adIndex = Array.IndexOf(baseTypes, typeof(IAsyncDisposable));
            var dIndex = Array.IndexOf(baseTypes, typeof(IDisposable));
            var hsIndex = Array.IndexOf(baseTypes, typeof(IHostedService));
            var oIndex = Array.IndexOf(baseTypes, typeof(object));
            oIndex.Should().Be(baseTypes.Length - 1);
            adwdsIndex.Should().BeLessThan(adIndex);
            adwdsIndex.Should().BeLessThan(dIndex);
            apIndex.Should().BeLessThan(adIndex);
            apIndex.Should().BeLessThan(hsIndex);
        }

        [Fact]
        public void ToIdentifierNameTest()
        {
            typeof(Tuple).ToIdentifierName().Should().Equals("Tuple");
            typeof(Tuple).ToIdentifierName(true).Should().Equals("System_Tuple");

            typeof(Tuple<>).ToIdentifierName().Should().Equals("Tuple_1");
            typeof(Tuple<>).ToIdentifierName(true).Should().Equals("System_Tuple_1");

            typeof(Tuple<object>).ToIdentifierName().Should().Equals("Tuple_Object");
            typeof(Tuple<object>).ToIdentifierName(true).Should().Equals("System_Tuple_Object");
            typeof(Tuple<object>).ToIdentifierName(true, true).Should().Equals("System_Tuple_System_Object");

            typeof(Dictionary<,>).ToIdentifierName().Should().Equals("Dictionary_2");
            typeof(Dictionary<,>).ToIdentifierName(true).Should().Equals("System_Collections_Generic_Dictionary_2");
            typeof(Dictionary<int, byte>).ToIdentifierName().Should().Equals("Dictionary_Int32_Byte");

            typeof(Tuple<Tuple<int>>).ToIdentifierName().Should().Equals("Tuple_Int_Int");
            typeof(Tuple<Tuple<int>>).ToIdentifierName(true).Should().Equals("System_Tuple_Box_Int_Int");
            typeof(Tuple<Tuple<int>>).ToIdentifierName(false, true).Should().Equals("Tuple_System_Int_System_Int");
            typeof(Tuple<Tuple<int>>).ToIdentifierName(true, true).Should().Equals("System_Tuple_System_Int_System_Int");
        }
    }
}
