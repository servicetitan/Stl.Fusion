namespace Stl.Serialization;

public interface ITextWriter
{
    string Write(object? value, Type type);
    ITextWriter<T> ToTyped<T>(Type? serializedType = null);
}

public interface ITextWriter<in T>
{
    string Write(T value);
}
