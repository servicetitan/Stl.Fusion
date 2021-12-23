using System.Buffers;

namespace Stl.Serialization;

public interface ITextSerializer
{
    bool PreferStringApi { get; }

    object? Read(string data, Type type);
    object? Read(ReadOnlyMemory<char> data, Type type);

    string Write(object? value, Type type);
    void Write(IBufferWriter<char> bufferWriter, object? value, Type type);

    ITextSerializer<T> ToTyped<T>(Type? serializedType = null);
}

public interface ITextSerializer<T>
{
    bool PreferStringApi { get; }

    T Read(string data);
    T Read(ReadOnlyMemory<char> data);

    string Write(T value);
    void Write(IBufferWriter<char> bufferWriter, T value);
}
