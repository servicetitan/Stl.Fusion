using System.Runtime.CompilerServices;

namespace Stl
{
    public static class IgnoreEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Ignore<T>(this T instance) { }
    }
}
