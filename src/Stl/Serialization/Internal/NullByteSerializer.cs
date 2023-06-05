using System.Buffers;

namespace Stl.Serialization.Internal;

/// <summary>
/// Ignores any operation.
/// </summary>
public sealed class NullByteSerializer : IByteSerializer
{
    public static NullByteSerializer Instance { get; } = new();

    public object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
    {
        readLength = 0;
        return null;
    }

    public object? Read(ReadOnlySequence<byte> data, Type type, out long readLength)
    {
        readLength = 0;
        return null;
    }

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
    public static NullByteSerializer<T> Instance { get; } = new();

    public T Read(ReadOnlyMemory<byte> data, out int readLength)
    {
        readLength = 0;
        return default!;
    }

    public T Read(ReadOnlySequence<byte> data, out long readLength)
    {
        readLength = 0;
        return default!;
    }

    public void Write(IBufferWriter<byte> bufferWriter, T value)
    { }
}
