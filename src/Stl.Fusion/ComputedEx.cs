using System.Runtime.CompilerServices;

namespace Stl.Fusion
{
    public static partial class ComputedEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComputedRef<TKey> ToRef<TKey>(
            this IComputedWithTypedInput<TKey> target)
            where TKey : notnull
            => new ComputedRef<TKey>(target.Function, target.Input, target.Tag);

        // Internal methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Strip<T>(this IComputed<T>? computed) 
            => computed != null ? computed.Value : default!;
    }
}
