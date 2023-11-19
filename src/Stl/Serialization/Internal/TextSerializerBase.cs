using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Cysharp.Text;
using Stl.Internal;

namespace Stl.Serialization.Internal;

public abstract class TextSerializerBase : ITextSerializer
{
    public bool PreferStringApi { get; protected init; } = true;

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public abstract object? Read(string data, Type type);
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public abstract string Write(object? value, Type type);

    // Read

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public virtual object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
    {
        var decoder = Encoding.UTF8.GetDecoder();
        var buffer = ZString.CreateStringBuilder();
        try {
            decoder.Convert(data.Span, ref buffer);
            readLength = data.Length;
            return Read(buffer.ToString(), type);
        }
        finally {
            buffer.Dispose();
        }
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public virtual object? Read(ReadOnlyMemory<char> data, Type type)
    {
#if NETSTANDARD2_0
        return Read(new string(data.ToArray()), type);
#else
        return Read(new string(data.Span), type);
#endif
    }

    // Write

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public virtual void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
    {
        var result = Write(value, type);
        var encoder = Encoding.UTF8.GetEncoder();
        encoder.Convert(result.AsSpan(), bufferWriter);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public virtual void Write(TextWriter textWriter, object? value, Type type)
    {
        var result = Write(value, type);
        textWriter.Write(result);
    }

    // ToTyped

    IByteSerializer<T> IByteSerializer.ToTyped<T>(Type? serializedType)
        => ToTyped<T>(serializedType);

    public virtual ITextSerializer<T> ToTyped<T>(Type? serializedType = null)
        => new CastingTextSerializer<T>(this, serializedType ?? typeof(T));
}
