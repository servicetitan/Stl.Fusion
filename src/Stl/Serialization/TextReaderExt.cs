namespace Stl.Serialization;

public static class TextReaderExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this ITextReader reader, string data)
        => (T) reader.Read(data, typeof(T))!;
}
