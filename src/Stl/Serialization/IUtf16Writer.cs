namespace Stl.Serialization;

public interface IUtf16Writer
{
    string Write(object? value, Type type);
    IUtf16Writer<T> ToTyped<T>(Type? serializedType = null);
}

public interface IUtf16Writer<in T>
{
    string Write(T value);
}
