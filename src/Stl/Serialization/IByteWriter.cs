using System;
using System.Buffers;

namespace Stl.Serialization
{
    public interface IByteWriter
    {
        void Write(IBufferWriter<byte> bufferWriter, object? value, Type type);
        IByteWriter<T> ToTyped<T>(Type? serializedType = null);
    }

    public interface IByteWriter<in T>
    {
        void Write(IBufferWriter<byte> bufferWriter, T? value);
    }
}
