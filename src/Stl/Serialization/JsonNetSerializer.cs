using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Stl.Serialization
{
    public class JsonNetSerializer : SerializerBase<string>
    {
        private readonly JsonSerializer _jsonSerializer;
        private readonly StringBuilder _stringBuilder;

        public static JsonSerializerSettings DefaultSettings => new JsonSerializerSettings() {
            SerializationBinder = CrossPlatformSerializationBinder.Instance,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new DefaultContractResolver(),
        };

        public JsonSerializerSettings Settings { get; }

        public JsonNetSerializer(JsonSerializerSettings? settings = null)
        {
            Settings = settings ??= DefaultSettings;
            _jsonSerializer = JsonSerializer.Create(settings);
            _stringBuilder = new StringBuilder(256);
        }

        public override string Serialize(object? native, Type? type)
        {
            _stringBuilder.Clear();
            using var stringWriter = new StringWriter(_stringBuilder);
            using var writer = new JsonTextWriter(stringWriter) {
                Formatting = _jsonSerializer.Formatting
            };
            // ReSharper disable once HeapView.BoxingAllocation
            _jsonSerializer.Serialize(writer, native, type);
            return _stringBuilder.ToString();
        }

        public override object? Deserialize(string serialized, Type? type)
        {
            using JsonTextReader reader = new JsonTextReader(new StringReader(serialized));
            return _jsonSerializer.Deserialize(reader, type);
        }
    }
}
