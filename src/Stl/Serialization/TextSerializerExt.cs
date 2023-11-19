using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Serialization;

public static class TextSerializerExt
{
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this ITextSerializer serializer, string data)
        => (T) serializer.Read(data, typeof(T))!;

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Write<T>(this ITextSerializer serializer, T value)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => serializer.Write(value, typeof(T));
}
