using Microsoft.Toolkit.HighPerformance.Buffers;

namespace Stl.IO.Internal;

internal static class ArrayPoolBufferWriterHelper<T>
{
    public static readonly Action<ArrayPoolBufferWriter<T>, int> IndexSetter;

    static ArrayPoolBufferWriterHelper()
    {
        var type = typeof(ArrayPoolBufferWriter<T>);
        var fIndex = type.GetField("index", BindingFlags.Instance | BindingFlags.NonPublic)!;
        IndexSetter = fIndex.GetSetter<ArrayPoolBufferWriter<T>, int>();
    }
}
