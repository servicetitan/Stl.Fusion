using System;
using Newtonsoft.Json;

namespace Stl.Serialization.Internal
{
    public class Base64EncodedNewtonsoftJsonConverter : JsonConverter<Base64Encoded>
    {
        public override void WriteJson(JsonWriter writer, Base64Encoded value, JsonSerializer serializer)
            => writer.WriteValue(value.Encode());

        public override Base64Encoded ReadJson(JsonReader reader, Type objectType, Base64Encoded existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => Base64Encoded.Decode((string?) reader.Value!);
    }
}
