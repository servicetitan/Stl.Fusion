using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Stl.Serialization.Internal;

namespace Stl.Serialization;

public class NewtonsoftJsonSerializer : TextSerializerBase
{
    private readonly JsonSerializer _jsonSerializer;
    private readonly StringBuilder _stringBuilder;

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
        _stringBuilder = new StringBuilder(256);
    }

    public override object? Read(string data, Type type)
    {
        using JsonTextReader reader = new(new StringReader(data));
        return _jsonSerializer.Deserialize(reader, type);
    }

    public override string Write(object? value, Type type)
    {
        _stringBuilder.Clear();
        using var stringWriter = new StringWriter(_stringBuilder);
        using var writer = new JsonTextWriter(stringWriter) {
            Formatting = _jsonSerializer.Formatting
        };
        // ReSharper disable once HeapView.BoxingAllocation
        _jsonSerializer.Serialize(writer, value, type);
        return _stringBuilder.ToString();
    }
}
