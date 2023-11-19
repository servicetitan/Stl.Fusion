using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Stl.Internal;
using Stl.IO;

namespace Stl.Serialization;

public static class ByteSerializerExt
{
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? Read(this IByteSerializer serializer, ReadOnlyMemory<byte> data, Type type)
        => serializer.Read(data, type, out _);

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? Read(this IByteSerializer serializer, ref ReadOnlyMemory<byte> data, Type type)
    {
        var result = serializer.Read(data, type, out var readLength);
        data = data[readLength..];
        return result;
    }

    // Read w/o Type & readLength arguments

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IByteSerializer serializer, ReadOnlyMemory<byte> data)
        => (T)serializer.Read(data, typeof(T), out _)!;

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IByteSerializer serializer, ref ReadOnlyMemory<byte> data)
    {
        var result = (T)serializer.Read(data, typeof(T), out var readLength)!;
        data = data[readLength..];
        return result;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IByteSerializer<T> serializer, ReadOnlyMemory<byte> data)
        => serializer.Read(data, out _);

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this IByteSerializer serializer, ReadOnlyMemory<byte> data, out int readLength)
        => (T)serializer.Read(data, typeof(T), out readLength)!;

    // Write w/o last Type argument

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(this IByteSerializer serializer, IBufferWriter<byte> bufferWriter, T value)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => serializer.Write(bufferWriter, value, typeof(T));

    // Write w/o IBufferWriter<byte> argument

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArrayPoolBuffer<byte> Write<T>(this IByteSerializer serializer, T value)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => serializer.Write(value, typeof(T));

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public static ArrayPoolBuffer<byte> Write(this IByteSerializer serializer, object? value, Type type)
    {
        var bufferWriter = new ArrayPoolBuffer<byte>();
        serializer.Write(bufferWriter, value, type);
        return bufferWriter;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public static ArrayPoolBuffer<byte> Write<T>(this IByteSerializer<T> serializer, T value)
    {
        var bufferWriter = new ArrayPoolBuffer<byte>();
        serializer.Write(bufferWriter, value);
        return bufferWriter;
    }
}
