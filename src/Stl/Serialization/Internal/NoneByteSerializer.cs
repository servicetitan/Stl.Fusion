using System.Buffers;

namespace Stl.Serialization.Internal;

/// <summary>
/// Throws an error on any operation.
/// </summary>
public sealed class NoneByteSerializer : IByteSerializer
{
    public static NoneByteSerializer Instance { get; } = new();

    public object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
        => throw Errors.NoSerializer();

    public void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
        => throw Errors.NoSerializer();

    public IByteSerializer<T> ToTyped<T>(Type? serializedType = null)
        => NoneByteSerializer<T>.Instance;
}

/// <summary>
/// Throws an error on any operation.
/// </summary>
public sealed class NoneByteSerializer<T> : IByteSerializer<T>
{
    public static NoneByteSerializer<T> Instance { get; } = new();

    public T Read(ReadOnlyMemory<byte> data, out int readLength)
        => throw Errors.NoSerializer();

    public void Write(IBufferWriter<byte> bufferWriter, T value)
        => throw Errors.NoSerializer();
}
