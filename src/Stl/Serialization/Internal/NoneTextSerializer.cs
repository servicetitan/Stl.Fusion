using System.Buffers;

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
    public object? Read(ReadOnlyMemory<char> data, Type type)
        => throw Errors.NoSerializer();

    public string Write(object? value, Type type)
        => throw Errors.NoSerializer();
    public void Write(IBufferWriter<char> bufferWriter, object? value, Type type)
        => throw Errors.NoSerializer();

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
    public T Read(ReadOnlyMemory<char> data)
        => throw Errors.NoSerializer();

    public string Write(T value)
        => throw Errors.NoSerializer();
    public void Write(IBufferWriter<char> bufferWriter, T value)
        => throw Errors.NoSerializer();
}
