using System.Buffers;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Stl.Serialization;

public static class ByteSerializerExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? Read(this IByteSerializer serializer, ReadOnlyMemory<byte> data, Type type)
        => serializer.Read(data, type, out _);

    // Read w/o Type & readLength arguments

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IByteSerializer serializer, ReadOnlyMemory<byte> data)
        => (T)serializer.Read(data, typeof(T), out _)!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IByteSerializer<T> serializer, ReadOnlyMemory<byte> data)
        => serializer.Read(data, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IByteSerializer serializer, ReadOnlyMemory<byte> data, out int readLength)
        => (T)serializer.Read(data, typeof(T), out readLength)!;

    // Write w/o last Type argument

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(this IByteSerializer serializer, IBufferWriter<byte> bufferWriter, T value)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => serializer.Write(bufferWriter, value, typeof(T));

    // Write w/o IBufferWriter<byte> argument

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArrayPoolBufferWriter<byte> Write<T>(this IByteSerializer serializer, T value)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => serializer.Write(value, typeof(T));

    public static ArrayPoolBufferWriter<byte> Write(this IByteSerializer serializer, object? value, Type type)
    {
        var bufferWriter = new ArrayPoolBufferWriter<byte>();
        serializer.Write(bufferWriter, value);
        return bufferWriter;
    }

    public static ArrayPoolBufferWriter<byte> Write<T>(this IByteSerializer<T> serializer, T value)
    {
        var bufferWriter = new ArrayPoolBufferWriter<byte>();
        serializer.Write(bufferWriter, value);
        return bufferWriter;
    }
}
