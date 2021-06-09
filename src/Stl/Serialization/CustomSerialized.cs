using System;

namespace Stl.Serialization
{
    public class CustomSerialized<T> : Serialized<T>
    {
        private Func<IUtf16Serializer<T>> SerializerFactory { get; set; }

        public CustomSerialized(Func<IUtf16Serializer<T>> serializerFactory)
            => SerializerFactory = serializerFactory;
        public CustomSerialized(Func<IUtf16Serializer<T>> serializerFactory, T value)
            : this(serializerFactory)
            => Value = value;
        public CustomSerialized(Func<IUtf16Serializer<T>> serializerFactory, string data)
            : this(serializerFactory)
            => Data = data;

        protected override IUtf16Serializer<T> CreateSerializer() => SerializerFactory.Invoke();
    }

    public static class CustomSerialized
    {
        public static CustomSerialized<T> New<T>(Func<IUtf16Serializer<T>> serializer) => new(serializer);
        public static CustomSerialized<T> New<T>(Func<IUtf16Serializer<T>> serializer, T value)
            => new(serializer, value);
        public static CustomSerialized<T> New<T>(Func<IUtf16Serializer<T>> serializer, string data)
            => new(serializer, data);
    }
}
