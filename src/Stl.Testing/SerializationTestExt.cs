using System.IO;
using System.Text.Json;
using FluentAssertions;
using MessagePack;
using Newtonsoft.Json;
using Stl.Reflection;
using Stl.Serialization;
using Xunit.Abstractions;

namespace Stl.Testing
{
    public static class SerializationTestExt
    {
        public static JsonSerializerOptions SystemJsonOptions;
        public static JsonSerializerSettings NewtonsoftJsonSettings;

        static SerializationTestExt()
        {
            SystemJsonOptions = new JsonSerializerOptions() { WriteIndented = true };
            NewtonsoftJsonSettings = MemberwiseCloner.Invoke(NewtonsoftJsonSerializer.DefaultSettings);
            NewtonsoftJsonSettings.Formatting = Formatting.Indented;
        }

        public static T AssertPassesThroughAllSerializers<T>(this T value, ITestOutputHelper? output = null)
        {
            var v = value;
            v = v.PassThroughSystemJsonSerializer(output);
            v.Should().Be(value);
            v = v.PassThroughNewtonsoftJsonSerializer(output);
            v.Should().Be(value);
            v = v.PassThroughTypeWritingSerializer(output);
            v.Should().Be(value);
            v = v.PassThroughSystemJsonSerialized(output);
            v.Should().Be(value);
            v = v.PassThroughNewtonsoftJsonSerialized(output);
            v.Should().Be(value);
            v = v.PassThroughMessagePackSerializer(output);
            v.Should().Be(value);
            return v;
        }

        public static T PassThroughAllSerializers<T>(this T value, ITestOutputHelper? output = null)
        {
            var v = value;
            v = v.PassThroughSystemJsonSerializer(output);
            v = v.PassThroughNewtonsoftJsonSerializer(output);
            v = v.PassThroughTypeWritingSerializer(output);
            v = v.PassThroughSystemJsonSerialized(output);
            v = v.PassThroughNewtonsoftJsonSerialized(output);
            v = v.PassThroughMessagePackSerializer(output);
            return v;
        }

        // TypeWritingSerializer

        public static T PassThroughTypeWritingSerializer<T>(this T value, ITestOutputHelper? output = null)
        {
            var sInner = new SystemJsonSerializer(SystemJsonOptions);
            var s = new TypeDecoratingSerializer(sInner);
            var json = s.Writer.Write(value, typeof(object));
            output?.WriteLine($"TypeWritingUtf16Serializer: {json}");
            return (T) s.Reader.Read<object>(json);
        }

        // System.Text.Json serializer

        public static T PassThroughSystemJsonSerializer<T>(this T value, ITestOutputHelper? output = null)
        {
            var s = new SystemJsonSerializer(SystemJsonOptions).ToTyped<T>();
            var json = s.Writer.Write(value);
            output?.WriteLine($"SystemJsonSerializer: {json}");
            return s.Reader.Read(json);
        }

        public static T PassThroughSystemJsonSerialized<T>(this T value, ITestOutputHelper? output = null)
        {
            var v1 = SystemJsonSerialized.New(value);
            output?.WriteLine($"SystemJsonSerialized: {v1.Data}");
            var v2 = SystemJsonSerialized.New<T>(v1.Data);
            return v2.Value;
        }

        // Newtonsoft.Json serializer

        public static T PassThroughNewtonsoftJsonSerializer<T>(this T value, ITestOutputHelper? output = null)
        {
            var s = new NewtonsoftJsonSerializer(NewtonsoftJsonSettings).ToTyped<T>();
            var json = s.Writer.Write(value);
            output?.WriteLine($"NewtonsoftJsonSerializer: {json}");
            return s.Reader.Read(json);
        }

        public static T PassThroughNewtonsoftJsonSerialized<T>(this T value, ITestOutputHelper? output = null)
        {
            var v1 = NewtonsoftJsonSerialized.New(value);
            output?.WriteLine($"NewtonsoftJsonSerialized: {v1.Data}");
            var v2 = NewtonsoftJsonSerialized.New<T>(v1.Data);
            return v2.Value;
        }

        // MessagePack serializer

        public static T PassThroughMessagePackSerializer<T>(this T value, ITestOutputHelper? output = null)
        {
            var options = MessagePackSerializer.DefaultOptions;
            using var ms = new MemoryStream();
            MessagePackSerializer.Serialize(ms, value, options);
            ms.Position = 0;
            var v1 = MessagePackSerializer.Deserialize<T>(ms, options);
            return v1;
        }
    }
}
