using System;
using Stl.Internal;
using Stl.Serialization;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Serialization
{
    public class SerializationTest : TestBase
    {
        public SerializationTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void BlazorTypeInfoSerializerTest()
        {
            var serializer = new JsonNetSerializer().ToTyped<Box<DateTime>>();
            var serialized = serializer.Serializer.Invoke(new Box<DateTime>(DateTime.Now));
            Out.WriteLine(serialized);

            var deserialized = serializer.Deserializer.Invoke(serialized);
            Out.WriteLine(deserialized.Value.ToString());
        }
    }
}
