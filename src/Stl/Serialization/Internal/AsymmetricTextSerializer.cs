using System.Buffers;

namespace Stl.Serialization.Internal;

public sealed class AsymmetricTextSerializer : ITextSerializer
{
    public ITextSerializer Reader { get; }
    public ITextSerializer Writer { get; }
    public bool PreferStringApi { get; }

    public AsymmetricTextSerializer(ITextSerializer reader, ITextSerializer writer, bool? preferStringApi = null)
    {
        Reader = reader;
        Writer = writer;
        PreferStringApi = preferStringApi ?? Reader.PreferStringApi;
    }

    IByteSerializer<T> IByteSerializer.ToTyped<T>(Type? serializedType)
        => ToTyped<T>(serializedType);
    public ITextSerializer<T> ToTyped<T>(Type? serializedType = null)
        => new AsymmetricTextSerializer<T>(
            Reader.ToTyped<T>(serializedType),
            Writer.ToTyped<T>(serializedType),
            PreferStringApi);

    public object? Read(string data, Type type)
        => Reader.Read(data, type);
    public object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
        => Reader.Read(data, type, out readLength);
    public object? Read(ReadOnlySequence<byte> data, Type type, out long readLength)
        => Reader.Read(data, type, out readLength);
    public object? Read(ReadOnlyMemory<char> data, Type type)
        => Reader.Read(data, type);

    public string Write(object? value, Type type)
        => Writer.Write(value, type);
    public void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
        => Writer.Write(bufferWriter, value, type);
    public void Write(TextWriter textWriter, object? value, Type type)
        => Writer.Write(textWriter, value, type);
}

public class AsymmetricTextSerializer<T> : ITextSerializer<T>
{
    public ITextSerializer<T> Reader { get; }
    public ITextSerializer<T> Writer { get; }
    public bool PreferStringApi { get; }

    public AsymmetricTextSerializer(ITextSerializer<T> reader, ITextSerializer<T> writer, bool? preferStringApi = null)
    {
        Reader = reader;
        Writer = writer;
        PreferStringApi = preferStringApi ?? Reader.PreferStringApi;
    }

    public T Read(string data)
        => Reader.Read(data);
    public T Read(ReadOnlyMemory<byte> data, out int readLength)
        => Reader.Read(data, out readLength);
    public T Read(ReadOnlySequence<byte> data, out long readLength)
        => Reader.Read(data, out readLength);
    public T Read(ReadOnlyMemory<char> data)
        => Reader.Read(data);

    public string Write(T value)
        => Writer.Write(value);
    public void Write(IBufferWriter<byte> bufferWriter, T value)
        => Writer.Write(bufferWriter, value);
    public void Write(TextWriter textWriter, T value)
        => Writer.Write(textWriter, value);
}
