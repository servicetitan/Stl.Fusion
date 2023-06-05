using System.Buffers;
using System.Text;
using Cysharp.Text;

namespace Stl.Serialization.Internal;

public class FuncTextSerializer<T> : ITextSerializer<T>
{
    public bool PreferStringApi => true;
    public Func<string, T> Reader { get; }
    public Func<T, string> Writer { get; }

    public FuncTextSerializer(Func<string, T> reader, Func<T, string> writer)
    {
        Reader = reader;
        Writer = writer;
    }

    // Read

    public T Read(string data)
        => Reader.Invoke(data);

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

    public T Read(ReadOnlySequence<byte> data, out long readLength)
    {
        var decoder = Encoding.UTF8.GetDecoder();
        var buffer = ZString.CreateStringBuilder();
        try {
            decoder.Convert(data, ref buffer);
            readLength = (int)data.Length;
            return Reader.Invoke(buffer.ToString());
        }
        finally {
            buffer.Dispose();
        }
    }

    public T Read(ReadOnlyMemory<char> data)
    {
#if NETSTANDARD2_0
        return Reader.Invoke(new string(data.ToArray()));
#else
        return Reader.Invoke(new string(data.Span));
#endif
    }

    // Write

    public string Write(T value)
        => Writer.Invoke(value);

    public void Write(IBufferWriter<byte> bufferWriter, T value)
    {
        var result = Writer.Invoke(value);
        var encoder = Encoding.UTF8.GetEncoder();
        encoder.Convert(result.AsSpan(), bufferWriter);
    }

    public void Write(TextWriter textWriter, T value)
    {
        var result = Writer.Invoke(value);
        textWriter.Write(result);
    }
}
