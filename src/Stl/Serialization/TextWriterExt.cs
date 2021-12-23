namespace Stl.Serialization;

public static class TextWriterExt
{
    public static string Write<T>(this ITextWriter writer, T value)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => writer.Write(value, typeof(T));
}
