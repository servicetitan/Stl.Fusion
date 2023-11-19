using System.Diagnostics.CodeAnalysis;
using Cysharp.Text;
using Microsoft.Toolkit.HighPerformance;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Stl.Internal;
using Stl.Serialization.Internal;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Stl.Serialization;

#if !NET5_0
[RequiresUnreferencedCode(UnreferencedCode.Serialization)]
#endif
public class NewtonsoftJsonSerializer : TextSerializerBase
{
    private readonly JsonSerializer _jsonSerializer;

    public static JsonSerializerSettings DefaultSettings { get; set; }

    public JsonSerializerSettings Settings { get; }

    public NewtonsoftJsonSerializer(JsonSerializerSettings? settings = null)
    {
        Settings = settings ??= DefaultSettings;
        _jsonSerializer = JsonSerializer.Create(settings);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
#pragma warning disable IL2116
#pragma warning disable CA2326, CA2327, CA2328
    static NewtonsoftJsonSerializer()
    {
        DefaultSettings = new JsonSerializerSettings {
#if !NET5_0_OR_GREATER
            SerializationBinder = CrossPlatformSerializationBinder.Instance,
#endif
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            DateParseHandling = DateParseHandling.None, // This makes sure all strings are deserialized as-is
            ContractResolver = new DefaultContractResolver(),
        };
    }
#pragma warning restore CA2326, CA2327, CA2328
#pragma warning restore IL2116

    // Read

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public override object? Read(string data, Type type)
        => _jsonSerializer.Deserialize(new StringReader(data), type);

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public override object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
    {
        using var stream = data.AsStream();
        using var reader = new StreamReader(stream);
        var result = _jsonSerializer.Deserialize(reader, type);
        readLength = (int)stream.Position;
        return result;
    }

    // Write

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public override string Write(object? value, Type type)
    {
        using var stringWriter = new ZStringWriter();
        using var writer = new JsonTextWriter(stringWriter);
        writer.Formatting = _jsonSerializer.Formatting;
        // ReSharper disable once HeapView.BoxingAllocation
        _jsonSerializer.Serialize(writer, value, type);
        return stringWriter.ToString();
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public override void Write(TextWriter textWriter, object? value, Type type)
    {
        using var writer = new JsonTextWriter(textWriter);
        writer.Formatting = _jsonSerializer.Formatting;
        // ReSharper disable once HeapView.BoxingAllocation
        _jsonSerializer.Serialize(writer, value, type);
    }
}
