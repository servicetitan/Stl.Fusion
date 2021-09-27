using System;
using System.Buffers;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Stl.Serialization
{
    public static class ByteWriterExt
    {
        // Write w/o last Type argument

        public static void Write<T>(this IByteWriter writer, IBufferWriter<byte> bufferWriter, T value)
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            => writer.Write(bufferWriter, value, typeof(T));

        // Write w/o IBufferWriter<byte> argument

        public static ArrayPoolBufferWriter<byte> Write<T>(this IByteWriter writer, T value)
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            => writer.Write(value, typeof(T));

        public static ArrayPoolBufferWriter<byte> Write(this IByteWriter writer, object? value, Type type)
        {
            var bufferWriter = new ArrayPoolBufferWriter<byte>();
            writer.Write(bufferWriter, value);
            return bufferWriter;
        }

        public static ArrayPoolBufferWriter<byte> Write<T>(this IByteWriter<T> writer, T value)
        {
            var bufferWriter = new ArrayPoolBufferWriter<byte>();
            writer.Write(bufferWriter, value);
            return bufferWriter;
        }
    }
}
