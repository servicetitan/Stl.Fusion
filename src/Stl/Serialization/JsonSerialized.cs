using System.Text.Json.Serialization;

namespace Stl.Serialization
{
    public static class JsonSerialized
    {
        public static JsonSerialized<TValue> New<TValue>() => new();
        public static JsonSerialized<TValue> New<TValue>(TValue value) => new(value);
        public static JsonSerialized<TValue> New<TValue>(string serializedValue) => new(serializedValue);
    }

    public class JsonSerialized<T> : Serialized<T>
    {
        public JsonSerialized() { }
        public JsonSerialized(T value) => Value = value;
        [JsonConstructor]
        public JsonSerialized(string data) => Data = data;

        protected override IUtf16Serializer<T> CreateSerializer()
            => new NewtonsoftJsonSerializer().ToTyped<T>();
    }
}
