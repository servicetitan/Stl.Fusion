namespace Stl.Serialization;

public interface ITextSerializer : IByteSerializer
{
    bool PreferStringApi { get; }

    object? Read(string data, Type type);
    object? Read(ReadOnlyMemory<char> data, Type type);
    string Write(object? value, Type type);
    void Write(TextWriter textWriter, object? value, Type type);

    new ITextSerializer<T> ToTyped<T>(Type? serializedType = null);
}

public interface ITextSerializer<T> : IByteSerializer<T>
{
    bool PreferStringApi { get; }

    T Read(string data);
    T Read(ReadOnlyMemory<char> data);
    string Write(T value);
    void Write(TextWriter textWriter, T value);
}
