using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Cysharp.Text;
using Stl.Internal;

namespace Stl.Serialization.Internal;

public class FuncTextSerializer<T>(Func<string, T> reader, Func<T, string> writer) : ITextSerializer<T>
{
    public bool PreferStringApi => true;
    public Func<string, T> Reader { get; } = reader;
    public Func<T, string> Writer { get; } = writer;

    // Read

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public T Read(string data)
        => Reader.Invoke(data);

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public T Read(ReadOnlyMemory<byte> data, out int readLength)
    {
        var decoder = Encoding.UTF8.GetDecoder();
        var buffer = ZString.CreateStringBuilder();
        try {
            decoder.Convert(data.Span, ref buffer);
            readLength = data.Length;
            return Reader.Invoke(buffer.ToString());
        }
        finally {
            buffer.Dispose();
        }
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public T Read(ReadOnlyMemory<char> data)
    {
#if NETSTANDARD2_0
        return Reader.Invoke(new string(data.ToArray()));
#else
        return Reader.Invoke(new string(data.Span));
#endif
    }

    // Write

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public string Write(T value)
        => Writer.Invoke(value);

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(IBufferWriter<byte> bufferWriter, T value)
    {
        var result = Writer.Invoke(value);
        var encoder = Encoding.UTF8.GetEncoder();
        encoder.Convert(result.AsSpan(), bufferWriter);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(TextWriter textWriter, T value)
    {
        var result = Writer.Invoke(value);
        textWriter.Write(result);
    }
}
