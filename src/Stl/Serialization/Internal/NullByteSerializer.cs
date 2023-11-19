using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Serialization.Internal;

/// <summary>
/// Ignores any operation.
/// </summary>
public sealed class NullByteSerializer : IByteSerializer
{
    public static readonly NullByteSerializer Instance = new();

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
    {
        readLength = 0;
        return null;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
    { }

    public IByteSerializer<T> ToTyped<T>(Type? serializedType = null)
        => NullByteSerializer<T>.Instance;
}

/// <summary>
/// Ignores any operation.
/// </summary>
public sealed class NullByteSerializer<T> : IByteSerializer<T>
{
    public static readonly NullByteSerializer<T> Instance = new();

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public T Read(ReadOnlyMemory<byte> data, out int readLength)
    {
        readLength = 0;
        return default!;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(IBufferWriter<byte> bufferWriter, T value)
    { }
}
