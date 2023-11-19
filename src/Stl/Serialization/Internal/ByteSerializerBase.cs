using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Serialization.Internal;

public abstract class ByteSerializerBase : IByteSerializer
{
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public abstract object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength);
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public abstract void Write(IBufferWriter<byte> bufferWriter, object? value, Type type);

    public virtual IByteSerializer<T> ToTyped<T>(Type? serializedType = null)
        => new CastingByteSerializer<T>(this, serializedType ?? typeof(T));
}
