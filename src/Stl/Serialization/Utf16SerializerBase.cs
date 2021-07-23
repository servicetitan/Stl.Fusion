using System;
using System.Buffers;
using Stl.Serialization.Internal;

namespace Stl.Serialization
{
    public abstract class Utf16SerializerBase : IUtf16Serializer, IUtf16Reader, IUtf16Writer
    {
        public IUtf16Reader Reader => this;
        public IUtf16Writer Writer => this;

        IUtf16Reader<T> IUtf16Reader.ToTyped<T>(Type? serializedType)
            => new CastingUtf16Serializer<T>(this, serializedType ?? typeof(T));
        IUtf16Writer<T> IUtf16Writer.ToTyped<T>(Type? serializedType)
            => new CastingUtf16Serializer<T>(this, serializedType ?? typeof(T));
        public virtual IUtf16Serializer<T> ToTyped<T>(Type? serializedType = null)
            => new CastingUtf16Serializer<T>(this, serializedType ?? typeof(T));

        public abstract object? Read(string data, Type type);
        public abstract string Write(object? value, Type type);
    }
}
