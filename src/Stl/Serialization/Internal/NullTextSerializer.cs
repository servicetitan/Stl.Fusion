using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Serialization.Internal;

/// <summary>
/// Ignores any operation.
/// </summary>
public sealed class NullTextSerializer : ITextSerializer
{
    public static readonly NullTextSerializer Instance = new();

    public bool PreferStringApi => false;

    // Read

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public object? Read(string data, Type type)
        => null;

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
    {
        readLength = 0;
        return null;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public object? Read(ReadOnlyMemory<char> data, Type type)
        => null;

    // Write

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public string Write(object? value, Type type)
        => "";

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
    { }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(TextWriter textWriter, object? value, Type type)
    { }

    // ToTyped

    IByteSerializer<T> IByteSerializer.ToTyped<T>(Type? serializedType)
        => ToTyped<T>(serializedType);

    public ITextSerializer<T> ToTyped<T>(Type? serializedType = null)
        => NullTextSerializer<T>.Instance;
}

/// <summary>
/// Ignores any operation.
/// </summary>
public sealed class NullTextSerializer<T> : ITextSerializer<T>
{
    public static readonly NullTextSerializer<T> Instance = new();

    public bool PreferStringApi => false;

    // Read

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public T Read(string data)
        => default!;

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public T Read(ReadOnlyMemory<byte> data, out int readLength)
    {
        readLength = 0;
        return default!;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public T Read(ReadOnlyMemory<char> data)
        => default!;

    // Write

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public string Write(T value)
        => "";

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(IBufferWriter<byte> bufferWriter, T value)
    { }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(TextWriter textWriter, T value)
    { }
}
