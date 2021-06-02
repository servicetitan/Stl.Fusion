#if NETFRAMEWORK

namespace System.Runtime.CompilerServices
{
    public static class RuntimeHelpers
    {
        public static T[] GetSubArray<T>(T[] array, System.Range range)
        {
            (int offset, int length) = range.GetOffsetAndLength(array.Length);
            var arr = new T[length];
            for (int i = 0; i < length; i++) {
                arr[i] = array[offset + i];
            }
            return arr;
        }
    }
}
#endif
