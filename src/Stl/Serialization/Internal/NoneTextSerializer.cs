using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Serialization.Internal;

/// <summary>
/// Throws an error on any operation.
/// </summary>
public sealed class NoneTextSerializer : ITextSerializer
{
    public static readonly NoneTextSerializer Instance = new();

    public bool PreferStringApi => false;

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public object? Read(string data, Type type)
        => throw Errors.NoSerializer();
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
        => throw Errors.NoSerializer();
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public object? Read(ReadOnlyMemory<char> data, Type type)
        => throw Errors.NoSerializer();

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public string Write(object? value, Type type)
        => throw Errors.NoSerializer();
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
        => throw Errors.NoSerializer();
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(TextWriter textWriter, object? value, Type type)
        => throw Errors.NoSerializer();

    IByteSerializer<T> IByteSerializer.ToTyped<T>(Type? serializedType)
        => ToTyped<T>(serializedType);
    public ITextSerializer<T> ToTyped<T>(Type? serializedType = null)
        => NoneTextSerializer<T>.Instance;
}

/// <summary>
/// Throws an error on any operation.
/// </summary>
public sealed class NoneTextSerializer<T> : ITextSerializer<T>
{
    public static readonly NoneTextSerializer<T> Instance = new();

    public bool PreferStringApi => false;

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public T Read(string data)
        => throw Errors.NoSerializer();
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public T Read(ReadOnlyMemory<byte> data, out int readLength)
        => throw Errors.NoSerializer();
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public T Read(ReadOnlyMemory<char> data)
        => throw Errors.NoSerializer();

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public string Write(T value)
        => throw Errors.NoSerializer();
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(IBufferWriter<byte> bufferWriter, T value)
        => throw Errors.NoSerializer();
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(TextWriter textWriter, T value)
        => throw Errors.NoSerializer();
}
