using System;

namespace Stl.Serialization
{
    public interface IUtf16Serializer
    {
        IUtf16Reader Reader { get; }
        IUtf16Writer Writer { get; }
        IUtf16Serializer<T> ToTyped<T>(Type? serializedType = null);
    }

    public interface IUtf16Serializer<T>
    {
        IUtf16Reader<T> Reader { get; }
        IUtf16Writer<T> Writer { get; }
    }
}
