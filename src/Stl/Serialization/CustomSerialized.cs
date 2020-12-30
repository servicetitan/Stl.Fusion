using System;

namespace Stl.Serialization
{
    public class CustomSerialized<TValue> : Serialized<TValue>
    {
        private Func<ISerializer<string>> SerializerFactory { get; set; }

        public CustomSerialized(Func<ISerializer<string>> serializerFactory)
            => SerializerFactory = serializerFactory;
        public CustomSerialized(Func<ISerializer<string>> serializerFactory, TValue value)
            : this(serializerFactory)
            => Value = value;
        public CustomSerialized(Func<ISerializer<string>> serializerFactory, string serializedValue)
            : this(serializerFactory)
            => SerializedValue = serializedValue;

        protected override ISerializer<string> CreateSerializer() => SerializerFactory.Invoke();
    }

    public static class CustomSerialized
    {
        public static CustomSerialized<TValue> New<TValue>(Func<ISerializer<string>> serializer) => new(serializer);
        public static CustomSerialized<TValue> New<TValue>(Func<ISerializer<string>> serializer, TValue value)
            => new(serializer, value);
        public static CustomSerialized<TValue> New<TValue>(Func<ISerializer<string>> serializer, string serializedValue)
            => new(serializer, serializedValue);
    }
}
