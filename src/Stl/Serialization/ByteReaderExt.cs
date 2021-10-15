using System;

namespace Stl.Serialization;

public static class ByteReaderExt
{
    public static T Read<T>(this IByteReader reader, ReadOnlyMemory<byte> data)
        => (T) reader.Read(data, typeof(T))!;
}
