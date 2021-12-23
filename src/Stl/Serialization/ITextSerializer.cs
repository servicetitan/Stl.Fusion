namespace Stl.Serialization;

public interface ITextSerializer
{
    ITextReader Reader { get; }
    ITextWriter Writer { get; }
    ITextSerializer<T> ToTyped<T>(Type? serializedType = null);
}

public interface ITextSerializer<T>
{
    ITextReader<T> Reader { get; }
    ITextWriter<T> Writer { get; }
}
