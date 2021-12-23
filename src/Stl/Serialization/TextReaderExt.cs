namespace Stl.Serialization;

public static class TextReaderExt
{
    public static T Read<T>(this ITextReader reader, string data)
        => (T) reader.Read(data, typeof(T))!;
}
