using System.Buffers;

namespace Stl.Serialization.Internal;

public abstract class TextSerializerBase : ITextSerializer
{
    public bool PreferStringApi { get; protected init; } = true;

    public abstract object? Read(string data, Type type);
    public abstract string Write(object? value, Type type);

#if NETSTANDARD2_0
    public virtual unsafe object? Read(ReadOnlyMemory<char> data, Type type)
    {
        fixed (char* dataPtr = &data.Span.GetPinnableReference())
            return Read(new string(dataPtr, 0, data.Length), type);
    }
#else
    public virtual object? Read(ReadOnlyMemory<char> data, Type type)
        => Read(new string(data.Span), type);
#endif

    public void Write(IBufferWriter<char> bufferWriter, object? value, Type type)
    {
        var result = Write(value, type);
        bufferWriter.Write(result.AsSpan());
    }

    public virtual ITextSerializer<T> ToTyped<T>(Type? serializedType = null)
        => new CastingTextSerializer<T>(this, serializedType ?? typeof(T));
}
