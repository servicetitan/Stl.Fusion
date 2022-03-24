using Microsoft.Extensions.Hosting;
using Stl.Reflection;

namespace Stl.Tests.Reflection;

public class TypeExtTest : TestBase
{
    public class Nested { }

    // ReSharper disable once UnusedMember.Global
    public static class NestedGeneric<T>
    {
        public class InnerNested { }
    }

    public TypeExtTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public void GetAllBaseTypesTest()
    {
        var baseTypes = GetType().GetAllBaseTypes().ToArray();
        baseTypes.Should().BeEquivalentTo(new [] {typeof(TestBase), typeof(object)});

        baseTypes = typeof(WorkerBase).GetAllBaseTypes(true, true).ToArray();
        var adbIndex = Array.IndexOf(baseTypes, typeof(AsyncDisposableBase));
        var apIndex = Array.IndexOf(baseTypes, typeof(IWorker));
        var adIndex = Array.IndexOf(baseTypes, typeof(IAsyncDisposable));
        var dIndex = Array.IndexOf(baseTypes, typeof(IDisposable));
        var hsIndex = Array.IndexOf(baseTypes, typeof(IHostedService));
        var hdsIndex = Array.IndexOf(baseTypes, typeof(IHasWhenDisposed));
        var oIndex = Array.IndexOf(baseTypes, typeof(object));
        adbIndex.Should().BeLessThan(adIndex);
        adbIndex.Should().BeLessThan(dIndex);
        apIndex.Should().BeLessThan(hdsIndex);
        apIndex.Should().BeLessThan(hsIndex);
        oIndex.Should().Be(baseTypes.Length - 1);
    }

    [Fact]
    public void ToIdentifierNameTest()
    {
        typeof(Tuple).ToIdentifierName().Should().Be("Tuple");
        typeof(Tuple).ToIdentifierName(true).Should().Be("System_Tuple");

        typeof(Tuple<>).ToIdentifierName().Should().Be("Tuple_1");
        typeof(Tuple<>).ToIdentifierName(true).Should().Be("System_Tuple_1");

        typeof(Tuple<object>).ToIdentifierName().Should().Be("Tuple_Object");
        typeof(Tuple<object>).ToIdentifierName(true).Should().Be("System_Tuple_Object");
        typeof(Tuple<object>).ToIdentifierName(true, true).Should().Be("System_Tuple_System_Object");

        typeof(Dictionary<,>).ToIdentifierName().Should().Be("Dictionary_2");
        typeof(Dictionary<,>).ToIdentifierName(true).Should().Be("System_Collections_Generic_Dictionary_2");
        typeof(Dictionary<int, byte>).ToIdentifierName().Should().Be("Dictionary_Int32_Byte");

        typeof(Tuple<Tuple<int>>).ToIdentifierName().Should().Be("Tuple_Tuple_Int32");
        typeof(Tuple<Tuple<int>>).ToIdentifierName(true).Should().Be("System_Tuple_Tuple_Int32");
        typeof(Tuple<Tuple<int>>).ToIdentifierName(false, true).Should().Be("Tuple_System_Tuple_System_Int32");
        typeof(Tuple<Tuple<int>>).ToIdentifierName(true, true).Should().Be("System_Tuple_System_Tuple_System_Int32");

        typeof(Nested).ToIdentifierName().Should().Be("TypeExtTest_Nested");
        typeof(Nested).ToIdentifierName(true).Should().Be("Stl_Tests_Reflection_TypeExtTest_Nested");

        typeof(NestedGeneric<>).ToIdentifierName().Should().Be("TypeExtTest_NestedGeneric_1");
        typeof(NestedGeneric<>).ToIdentifierName(true).Should().Be("Stl_Tests_Reflection_TypeExtTest_NestedGeneric_1");

        typeof(NestedGeneric<>.InnerNested).ToIdentifierName().Should().Be("TypeExtTest_NestedGeneric_1_InnerNested_1");
        typeof(NestedGeneric<>.InnerNested).ToIdentifierName(true).Should().Be("Stl_Tests_Reflection_TypeExtTest_NestedGeneric_1_InnerNested_1");
    }

    [Fact]
    public void GetNameTest()
    {
        typeof(Tuple).GetName().Should().Be("Tuple");
        typeof(Tuple).GetName(true).Should().Be("System.Tuple");

        typeof(Tuple<>).GetName().Should().Be("Tuple<T1>");
        typeof(Tuple<>).GetName(true).Should().Be("System.Tuple<T1>");

        typeof(Tuple<object>).GetName().Should().Be("Tuple<Object>");
        typeof(Tuple<object>).GetName(true).Should().Be("System.Tuple<Object>");
        typeof(Tuple<object>).GetName(true, true).Should().Be("System.Tuple<System.Object>");

        typeof(Dictionary<,>).GetName().Should().Be("Dictionary<TKey,TValue>");
        typeof(Dictionary<,>).GetName(true).Should().Be("System.Collections.Generic.Dictionary<TKey,TValue>");
        typeof(Dictionary<int, byte>).GetName().Should().Be("Dictionary<Int32,Byte>");

        typeof(Tuple<Tuple<int>>).GetName().Should().Be("Tuple<Tuple<Int32>>");
        typeof(Tuple<Tuple<int>>).GetName(true).Should().Be("System.Tuple<Tuple<Int32>>");
        typeof(Tuple<Tuple<int>>).GetName(false, true).Should().Be("Tuple<System.Tuple<System.Int32>>");
        typeof(Tuple<Tuple<int>>).GetName(true, true).Should().Be("System.Tuple<System.Tuple<System.Int32>>");

        typeof(Nested).GetName().Should().Be("TypeExtTest+Nested");
        typeof(Nested).GetName(true).Should().Be("Stl.Tests.Reflection.TypeExtTest+Nested");

        typeof(NestedGeneric<>).GetName().Should().Be("TypeExtTest+NestedGeneric<T>");
        typeof(NestedGeneric<>).GetName(true).Should().Be("Stl.Tests.Reflection.TypeExtTest+NestedGeneric<T>");

        typeof(NestedGeneric<>.InnerNested).GetName().Should().Be("TypeExtTest+NestedGeneric<T>+InnerNested<T>");
        typeof(NestedGeneric<>.InnerNested).GetName(true).Should().Be("Stl.Tests.Reflection.TypeExtTest+NestedGeneric<T>+InnerNested<T>");
    }
}
