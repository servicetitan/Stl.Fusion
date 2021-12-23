namespace Stl.Serialization.Internal;

public abstract class TextSerializerBase : ITextSerializer, ITextReader, ITextWriter
{
    public ITextReader Reader => this;
    public ITextWriter Writer => this;

    ITextReader<T> ITextReader.ToTyped<T>(Type? serializedType)
        => new CastingTextSerializer<T>(this, serializedType ?? typeof(T));
    ITextWriter<T> ITextWriter.ToTyped<T>(Type? serializedType)
        => new CastingTextSerializer<T>(this, serializedType ?? typeof(T));
    public virtual ITextSerializer<T> ToTyped<T>(Type? serializedType = null)
        => new CastingTextSerializer<T>(this, serializedType ?? typeof(T));

    public abstract object? Read(string data, Type type);
    public abstract string Write(object? value, Type type);
}
