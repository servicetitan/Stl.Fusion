using System;

namespace Stl.Serialization
{
    public interface IByteSerializer
    {
        IByteReader Reader { get; }
        IByteWriter Writer { get; }
        IByteSerializer<T> ToTyped<T>(Type? serializedType = null);
    }

    public interface IByteSerializer<T>
    {
        IByteReader<T> Reader { get; }
        IByteWriter<T> Writer { get; }
    }
}
