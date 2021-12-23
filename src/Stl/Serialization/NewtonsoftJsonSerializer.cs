using System.Buffers;
using System.Text;
using Cysharp.Text;
using Microsoft.Toolkit.HighPerformance;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Stl.Serialization.Internal;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Stl.Serialization;

public class NewtonsoftJsonSerializer : TextSerializerBase
{
    private readonly JsonSerializer _jsonSerializer;

    public static JsonSerializerSettings DefaultSettings { get; set; } =
        new() {
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

    public JsonSerializerSettings Settings { get; }

    public NewtonsoftJsonSerializer(JsonSerializerSettings? settings = null)
    {
        Settings = settings ??= DefaultSettings;
        _jsonSerializer = JsonSerializer.Create(settings);
    }

    public override object? Read(string data, Type type)
        => _jsonSerializer.Deserialize(new StringReader(data), type);
    public override object? Read(ReadOnlyMemory<byte> data, Type type)
        => _jsonSerializer.Deserialize(new StreamReader(data.AsStream()), type);

    public override string Write(object? value, Type type)
    {
        using var stringWriter = new ZStringWriter();
        using var writer = new JsonTextWriter(stringWriter) {
            Formatting = _jsonSerializer.Formatting
        };
        // ReSharper disable once HeapView.BoxingAllocation
        _jsonSerializer.Serialize(writer, value, type);
        return stringWriter.ToString();
    }
}
