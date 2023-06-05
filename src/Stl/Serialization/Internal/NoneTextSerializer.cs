using System.Buffers;
using System.Text;

namespace Stl.Serialization.Internal;

/// <summary>
/// Throws an error on any operation.
/// </summary>
public sealed class NoneTextSerializer : ITextSerializer
{
    public static NoneTextSerializer Instance { get; } = new();

    public bool PreferStringApi => false;

    public object? Read(string data, Type type)
        => throw Errors.NoSerializer();
    public object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
        => throw Errors.NoSerializer();
    public object? Read(ReadOnlyMemory<char> data, Type type)
        => throw Errors.NoSerializer();

    public string Write(object? value, Type type)
        => throw Errors.NoSerializer();
    public void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
        => throw Errors.NoSerializer();
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
    public static NoneTextSerializer<T> Instance { get; } = new();

    public bool PreferStringApi => false;

    public T Read(string data)
        => throw Errors.NoSerializer();
    public T Read(ReadOnlyMemory<byte> data, out int readLength)
        => throw Errors.NoSerializer();
    public T Read(ReadOnlyMemory<char> data)
        => throw Errors.NoSerializer();

    public string Write(T value)
        => throw Errors.NoSerializer();
    public void Write(IBufferWriter<byte> bufferWriter, T value)
        => throw Errors.NoSerializer();
    public void Write(TextWriter textWriter, T value)
        => throw Errors.NoSerializer();
}
