using System.Runtime.CompilerServices;

namespace Stl.Fusion
{
    public static partial class ComputedEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Strip<T>(this IComputed<T>? computed) 
            => computed != null ? computed.Value : default!;
    }
}
