using System;
using Newtonsoft.Json;

namespace Stl.Fusion.Authentication.Internal
{
    public class AuthContextJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(AuthContext);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var symbol = (AuthContext?) value!;
            writer.WriteValue(symbol?.Id);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = (string?) reader.Value!;
            return value == null ? null : new AuthContext(value);
        }
    }
}
