using System.Text;
using FluentAssertions;
using Microsoft.Toolkit.HighPerformance.Buffers;
using Newtonsoft.Json;
using Stl.IO;
using Xunit.Abstractions;

namespace Stl.Testing;

public class TypeEvolutionTester<TOld, TNew>
{
    public Action<TOld, TNew> AssertEqual { get; }
    public JsonSerializerOptions SystemJsonOptions { get; set; }
    public JsonSerializerSettings NewtonsoftJsonSettings { get; set; }
    public ITestOutputHelper? Output { get; }

    public TypeEvolutionTester(Action<TOld, TNew> assertEqual, ITestOutputHelper? output = null)
    {
        AssertEqual = assertEqual;
        Output = output;
        SystemJsonOptions = MemberwiseCloner.Invoke(SystemJsonSerializer.DefaultOptions);
        NewtonsoftJsonSettings = MemberwiseCloner.Invoke(NewtonsoftJsonSerializer.DefaultSettings);
        NewtonsoftJsonSettings.Formatting = Formatting.Indented;
    }

    public void CheckAllSerializers(TOld value)
    {
        CheckSystemJsonSerializer(value);
        CheckNewtonsoftJsonSerializer(value);
        CheckMessagePackByteSerializer(value);
        CheckMemoryPackByteSerializer(value);
    }

    // System.Text.Json serializer

    public void CheckSystemJsonSerializer(TOld value)
    {
        var s = new SystemJsonSerializer(SystemJsonOptions);
        var json = s.Write(value);
        Output?.WriteLine($"SystemJsonSerializer: {json}");
        var v0 = s.Read<TNew>(json);
        AssertEqual(value, v0);

        using var buffer = new ArrayPoolBuffer<byte>();
        s.Write(buffer, value);
        var bytes = buffer.WrittenMemory;
        var json2 = Encoding.UTF8.GetDecoder().Convert(bytes.Span);
        json2.Should().Be(json);
        var v1 = s.Read<TNew>(bytes);
        AssertEqual(value, v1);
    }

    // Newtonsoft.Json serializer

    public void CheckNewtonsoftJsonSerializer(TOld value, ITestOutputHelper? output = null)
    {
        var s = new NewtonsoftJsonSerializer(NewtonsoftJsonSettings);
        var json = s.Write(value);
        Output?.WriteLine($"NewtonsoftJsonSerializer: {json}");
        var v0 = s.Read<TNew>(json);
        AssertEqual(value, v0);

        using var buffer = new ArrayPoolBuffer<byte>();
        s.Write(buffer, value);
        var bytes = buffer.WrittenMemory;
        var json2 = Encoding.UTF8.GetDecoder().Convert(bytes.Span);
        json2.Should().Be(json);
        var v1 = s.Read<TNew>(bytes);
        AssertEqual(value, v1);
    }

    // MessagePack serializer

    public void CheckMessagePackByteSerializer(TOld value, ITestOutputHelper? output = null)
    {
        var s = new MessagePackByteSerializer();
        using var buffer = s.Write(value);
        var bytes = buffer.WrittenMemory.ToArray();
        Output?.WriteLine($"MessagePackByteSerializer: {JsonFormatter.Format(bytes)}");
        s.Write(buffer, value);

        bytes = buffer.WrittenMemory.ToArray();
        var v0 = s.Read<TNew>(bytes, out var readLength);
        AssertEqual(value, v0);
        var v1 = s.Read<TNew>(bytes[readLength..]);
        AssertEqual(value, v1);
    }

    // MemoryPack serializer

    public void CheckMemoryPackByteSerializer(TOld value, ITestOutputHelper? output = null)
    {
        var s = new MemoryPackByteSerializer();
        using var buffer = s.Write(value);
        var bytes = buffer.WrittenMemory.ToArray();
        Output?.WriteLine($"MemoryPackByteSerializer: {JsonFormatter.Format(bytes)}");
        s.Write(buffer, value);

        bytes = buffer.WrittenMemory.ToArray();
        var v0 = s.Read<TNew>(bytes, out var readLength);
        AssertEqual(value, v0);
        var v1 = s.Read<TNew>(bytes[readLength..]);
        AssertEqual(value, v1);
    }
}
