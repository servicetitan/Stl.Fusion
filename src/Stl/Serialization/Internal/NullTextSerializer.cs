using System.Buffers;

namespace Stl.Serialization.Internal;

/// <summary>
/// Ignores any operation.
/// </summary>
public sealed class NullTextSerializer : ITextSerializer
{
    public static readonly NullTextSerializer Instance = new();

    public bool PreferStringApi => false;

    // Read

    public object? Read(string data, Type type)
        => null;

    public object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
    {
        readLength = 0;
        return null;
    }

    public object? Read(ReadOnlyMemory<char> data, Type type)
        => null;

    // Write

    public string Write(object? value, Type type)
        => "";

    public void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
    { }

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

    public T Read(string data)
        => default!;

    public T Read(ReadOnlyMemory<byte> data, out int readLength)
    {
        readLength = 0;
        return default!;
    }

    public T Read(ReadOnlyMemory<char> data)
        => default!;

    // Write

    public string Write(T value)
        => "";

    public void Write(IBufferWriter<byte> bufferWriter, T value)
    { }

    public void Write(TextWriter textWriter, T value)
    { }
}
