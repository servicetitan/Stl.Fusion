namespace Stl.Serialization;

public interface ITextSerializer : ITextReader, ITextWriter
{
    ITextReader Reader { get; }
    ITextWriter Writer { get; }
    new ITextSerializer<T> ToTyped<T>(Type? serializedType = null);
}

public interface ITextSerializer<T> : ITextReader<T>, ITextWriter<T>
{
    ITextReader<T> Reader { get; }
    ITextWriter<T> Writer { get; }
}
