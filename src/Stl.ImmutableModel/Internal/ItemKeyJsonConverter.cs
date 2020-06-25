using System;
using Newtonsoft.Json;
using Stl.ImmutableModel.Reflection;

namespace Stl.ImmutableModel.Internal 
{
    public class ItemKeyJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) 
            => objectType == typeof(ItemKey);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var keyOrSymbol = (ItemKey) value!;
            writer.WriteValue(keyOrSymbol.Format());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = (string) reader.Value!;
            return ItemKey.Parse(value);
        }
    }
}
