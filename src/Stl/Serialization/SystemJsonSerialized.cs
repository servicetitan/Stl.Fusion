using System;
using System.Runtime.Serialization;

namespace Stl.Serialization
{
    public static class SystemJsonSerialized
    {
        public static SystemJsonSerialized<TValue> New<TValue>() => new();
        public static SystemJsonSerialized<TValue> New<TValue>(TValue value) => new() { Value = value };
        public static SystemJsonSerialized<TValue> New<TValue>(string serializedValue) => new(serializedValue);
    }

    [DataContract]
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
    public class SystemJsonSerialized<T> : Serialized<T>
    {
        [ThreadStatic] private static IUtf16Serializer<T>? _serializer;

        public SystemJsonSerialized() { }
        public SystemJsonSerialized(string data) => Data = data;

        protected override IUtf16Serializer<T> GetSerializer()
            => _serializer ??= SystemJsonSerializer.Default.ToTyped<T>();
    }
}
