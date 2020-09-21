using System;
using Newtonsoft.Json;

namespace Stl.Fusion.Authentication.Internal
{
    public class SessionJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(AuthSession);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var symbol = (AuthSession?) value!;
            writer.WriteValue(symbol?.Id);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = (string?) reader.Value!;
            return value == null ? null : new AuthSession(value);
        }
    }
}
