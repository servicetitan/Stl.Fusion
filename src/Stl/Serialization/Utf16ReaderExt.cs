namespace Stl.Serialization;

public static class Utf16ReaderExt
{
    public static T Read<T>(this IUtf16Reader reader, string data)
        => (T) reader.Read(data, typeof(T))!;
}
