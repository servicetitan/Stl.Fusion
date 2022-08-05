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

    public T Read(ReadOnlyMemory<byte> data)
    {
        var decoder = Encoding.UTF8.GetDecoder();
        var buffer = ZString.CreateStringBuilder();
        try {
            decoder.Convert(data.Span, ref buffer);
            return Reader.Invoke(buffer.ToString());
        }
        finally {
            buffer.Dispose();
        }
    }

    public void Write(IBufferWriter<byte> bufferWriter, T value)
    {
        var result = Writer.Invoke(value);
        var encoder = Encoding.UTF8.GetEncoder();
        encoder.Convert(result.AsSpan(), bufferWriter);
    }

    public T Read(string data)
        => Reader.Invoke(data);

    public string Write(T value)
        => Writer.Invoke(value);
}
