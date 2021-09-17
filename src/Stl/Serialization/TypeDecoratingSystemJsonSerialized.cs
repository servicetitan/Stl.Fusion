using System;
using System.Runtime.Serialization;

namespace Stl.Serialization
{
    public static class TypeDecoratingSystemJsonSerialized
    {
        public static TypeDecoratingSystemJsonSerialized<TValue> New<TValue>() => new();
        public static TypeDecoratingSystemJsonSerialized<TValue> New<TValue>(TValue value) => new(value);
        public static TypeDecoratingSystemJsonSerialized<TValue> New<TValue>(string serializedValue) => new(serializedValue);
    }

    [DataContract]
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
    public class TypeDecoratingSystemJsonSerialized<T> : Serialized<T>
    {
        [ThreadStatic] private static IUtf16Serializer<T>? _serializer;

        public TypeDecoratingSystemJsonSerialized() { }
        public TypeDecoratingSystemJsonSerialized(T value) => Value = value;
        public TypeDecoratingSystemJsonSerialized(string data) => Data = data;

        protected override IUtf16Serializer<T> GetSerializer()
            => _serializer ??= new TypeDecoratingSerializer(SystemJsonSerializer.Default).ToTyped<T>();
    }
}
