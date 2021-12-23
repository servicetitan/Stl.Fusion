namespace Stl.Serialization;

public interface ITextReader
{
    object? Read(string data, Type type);
    ITextReader<T> ToTyped<T>(Type? serializedType = null);
}

// ReSharper disable once TypeParameterCanBeVariant
public interface ITextReader<T>
{
    T Read(string data);
}
