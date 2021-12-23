namespace Stl.Serialization;

public static class TextWriterExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Write<T>(this ITextWriter writer, T value)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => writer.Write(value, typeof(T));
}
