using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Common;
using Newtonsoft.Json;
using Stl.Internal;
using Stl.Reflection;
using Stl.Serialization;
using Stl.Serialization.Internal;
using Xunit.Abstractions;

namespace Stl.Testing
{
    public static class SerializationTestEx
    {
        public static JsonSerializerOptions SystemJsonOptions;
        public static JsonSerializerSettings NewtonsoftJsonSettings;

        static SerializationTestEx()
        {
            SystemJsonOptions = new JsonSerializerOptions() { WriteIndented = true };
            NewtonsoftJsonSettings = MemberwiseCloner.Invoke(NewtonsoftJsonSerializer.DefaultSettings);
            NewtonsoftJsonSettings.Formatting = Formatting.Indented;
        }

        public static T AssertPassesThroughAllSerializers<T>(this T value, ITestOutputHelper? output = null)
        {
            var v = value;
            v = v.PassThroughSystemJsonSerializer(output);
            v.Should().IsSameOrEqualTo(value);
            v = v.PassThroughNewtonsoftJsonSerializer(output);
            v.Should().IsSameOrEqualTo(value);
            v = v.PassThroughTypeWritingSerializer(output);
            v.Should().IsSameOrEqualTo(value);
            v = v.PassThroughSystemJsonSerialized(output);
            v.Should().IsSameOrEqualTo(value);
            v = v.PassThroughNewtonsoftJsonSerialized(output);
            v.Should().IsSameOrEqualTo(value);
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
            var v2 = NewtonsoftJsonSerialized.New<T>(v1.Data);
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
    }
}
