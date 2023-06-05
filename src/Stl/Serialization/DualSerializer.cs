using System.Buffers;
using Microsoft.Toolkit.HighPerformance.Buffers;
using Stl.Serialization.Internal;

namespace Stl.Serialization;

public sealed record DualSerializer<T>(
    SerializedFormat DefaultFormat,
    IByteSerializer<T> ByteSerializer,
    ITextSerializer<T> TextSerializer)
{
    public DualSerializer()
        : this(SerializedFormat.Bytes)
    { }

    public DualSerializer(SerializedFormat DefaultFormat)
        : this(
            DefaultFormat,
            Serialization.ByteSerializer.Default.ToTyped<T>(),
            Serialization.TextSerializer.Default.ToTyped<T>())
    { }

    public DualSerializer(IByteSerializer<T> byteSerializer)
        : this(SerializedFormat.Bytes, byteSerializer, NoneTextSerializer<T>.Instance)
    { }

    public DualSerializer(ITextSerializer<T> textSerializer)
        : this(SerializedFormat.Text, NoneByteSerializer<T>.Instance, textSerializer)
    { }

    // Read

    public T Read(Serialized data)
        => data.Format == SerializedFormat.Text
            ? TextSerializer.Read(data.Data)
            : ByteSerializer.Read(data.Data, out _);

    public T Read(ReadOnlyMemory<byte> data)
        => Read(data, DefaultFormat);
    public T Read(ReadOnlyMemory<byte> data, SerializedFormat format)
        => format == SerializedFormat.Text
            ? TextSerializer.Read(data, out _)
            : ByteSerializer.Read(data, out _);

    public T Read(ReadOnlySequence<byte> data)
        => Read(data, DefaultFormat);
    public T Read(ReadOnlySequence<byte> data, SerializedFormat format)
        => format == SerializedFormat.Text
            ? TextSerializer.Read(data, out _)
            : ByteSerializer.Read(data, out _);

    // Write

    public Serialized Write(T value)
        => Write(value, DefaultFormat);
    public Serialized Write(T value, SerializedFormat format)
    {
        using var bufferWriter = new ArrayPoolBufferWriter<byte>();
        if (format == SerializedFormat.Text)
            TextSerializer.Write(bufferWriter, value);
        else
            ByteSerializer.Write(bufferWriter, value);
        return new Serialized(bufferWriter.WrittenMemory.ToArray(), format);
    }

    public void Write(T value, IBufferWriter<byte> bufferWriter)
        => Write(value, DefaultFormat, bufferWriter);
    public void Write(T value, SerializedFormat format, IBufferWriter<byte> bufferWriter)
    {
        if (format == SerializedFormat.Text)
            TextSerializer.Write(bufferWriter, value);
        else
            ByteSerializer.Write(bufferWriter, value);
    }
}
