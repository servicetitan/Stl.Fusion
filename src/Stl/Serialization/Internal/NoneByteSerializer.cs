using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Serialization.Internal;

/// <summary>
/// Throws an error on any operation.
/// </summary>
public sealed class NoneByteSerializer : IByteSerializer
{
    public static readonly NoneByteSerializer Instance = new();

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
        => throw Errors.NoSerializer();

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
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
    public static readonly NoneByteSerializer<T> Instance = new();

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public T Read(ReadOnlyMemory<byte> data, out int readLength)
        => throw Errors.NoSerializer();

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(IBufferWriter<byte> bufferWriter, T value)
        => throw Errors.NoSerializer();
}
