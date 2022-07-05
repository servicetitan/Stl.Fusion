namespace Stl.Serialization;

public interface ITextSerializer : IByteSerializer
{
    bool PreferStringApi { get; }

    object? Read(string data, Type type);
    string Write(object? value, Type type);

    new ITextSerializer<T> ToTyped<T>(Type? serializedType = null);
}

public interface ITextSerializer<T> : IByteSerializer<T>
{
    bool PreferStringApi { get; }

    T Read(string data);
    string Write(T value);
}
