using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

#if !NET5_0_OR_GREATER
/// <summary>
/// Reserved to be used by the compiler for tracking metadata.
/// This class should not be used by developers in source code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit { }
#endif

#if NETFRAMEWORK
public static class RuntimeHelpers
{
    public static T[] GetSubArray<T>(T[] array, Range range)
    {
        (int offset, int length) = range.GetOffsetAndLength(array.Length);
        var arr = new T[length];
        for (int i = 0; i < length; i++)
            arr[i] = array[offset + i];
        return arr;
    }
}
#endif
