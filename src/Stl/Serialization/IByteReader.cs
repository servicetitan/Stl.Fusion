namespace Stl.Serialization;

public interface IByteReader
{
    object? Read(ReadOnlyMemory<byte> data, Type type);
    IByteReader<T> ToTyped<T>(Type? serializedType = null);
}

// ReSharper disable once TypeParameterCanBeVariant
public interface IByteReader<T>
{
    T Read(ReadOnlyMemory<byte> data);
}
