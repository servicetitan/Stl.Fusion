namespace Stl.Serialization;

public interface IUtf16Reader
{
    object? Read(string data, Type type);
    IUtf16Reader<T> ToTyped<T>(Type? serializedType = null);
}

// ReSharper disable once TypeParameterCanBeVariant
public interface IUtf16Reader<T>
{
    T Read(string data);
}
