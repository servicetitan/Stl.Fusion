using System;
using Newtonsoft.Json;
using Stl.ImmutableModel.Indexing;

namespace Stl.ImmutableModel.Internal 
{
    public class NodeLinkJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) 
            => objectType == typeof(NodeLink);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var keyOrSymbol = (NodeLink) value!;
            writer.WriteValue(keyOrSymbol.Format());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = (string) reader.Value!;
            return NodeLink.Parse(value);
        }
    }
}
