using System;
using System.Runtime.Serialization;

namespace Stl.Serialization
{
    public static class NewtonsoftJsonSerialized
    {
        public static NewtonsoftJsonSerialized<TValue> New<TValue>() => new();
        public static NewtonsoftJsonSerialized<TValue> New<TValue>(TValue value) => new() { Value = value };
        public static NewtonsoftJsonSerialized<TValue> New<TValue>(string serializedValue) => new(serializedValue);
    }

    [DataContract]
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
    public class NewtonsoftJsonSerialized<T> : Serialized<T>
    {
        [ThreadStatic] private static IUtf16Serializer<T>? _serializer;

        public NewtonsoftJsonSerialized() { }
        public NewtonsoftJsonSerialized(string data) => Data = data;

        protected override IUtf16Serializer<T> GetSerializer()
            => _serializer ??= new NewtonsoftJsonSerializer().ToTyped<T>();
    }
}
