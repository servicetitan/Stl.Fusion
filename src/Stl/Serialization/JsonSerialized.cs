using System.Text.Json.Serialization;

namespace Stl.Serialization
{
    public static class JsonSerialized
    {
        public static JsonSerialized<TValue> New<TValue>() => new();
        public static JsonSerialized<TValue> New<TValue>(TValue value) => new(value);
        public static JsonSerialized<TValue> New<TValue>(string serializedValue) => new(serializedValue);
    }

    public class JsonSerialized<TValue> : Serialized<TValue>
    {
        public JsonSerialized() { }
        public JsonSerialized(TValue value) => Value = value;
        [JsonConstructor]
        public JsonSerialized(string serializedValue) => SerializedValue = serializedValue;

        protected override ISerializer<string> CreateSerializer() => new JsonNetSerializer();
    }
}
