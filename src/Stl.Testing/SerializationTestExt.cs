using System.Text.Json;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Stl.Testing;

public static class SerializationTestExt
{
    public static JsonSerializerOptions SystemJsonOptions { get; set; }
    public static JsonSerializerSettings NewtonsoftJsonSettings { get; set; }

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
        v = v.PassThroughMessagePackByteSerializer(output);
        v.Should().Be(value);
        v = v.PassThroughMessagePackSerialized(output);
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
        v = v.PassThroughMessagePackByteSerializer(output);
        v = v.PassThroughMessagePackSerialized(output);
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

    public static T PassThroughMessagePackByteSerializer<T>(this T value, ITestOutputHelper? output = null)
    {
        var s = new MessagePackByteSerializer().ToTyped<T>();
        using var bufferWriter = s.Writer.Write(value);
        var data = bufferWriter.WrittenMemory.ToArray();
        output?.WriteLine($"MessagePackByteSerializer: {JsonFormatter.Format(data)}");
        var v1 = s.Reader.Read(data);
        return v1;
    }

    public static T PassThroughMessagePackSerialized<T>(this T value, ITestOutputHelper? output = null)
    {
        var v1 = MessagePackSerialized.New(value);
        output?.WriteLine($"MessagePackSerialized: {v1.Data}");
        var v2 = MessagePackSerialized.New<T>(v1.Data);
        return v2.Value;
    }
}
