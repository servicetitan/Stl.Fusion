using System.Runtime.CompilerServices;

namespace Stl.Fusion
{
    public static partial class ComputedEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComputedRef ToRef<TKey>(
            this IComputedWithTypedInput<TKey> target)
            where TKey : class
            => new ComputedRef(target.Function, target.Input);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TaggedComputedRef ToTaggedRef<TKey>(
            this IComputedWithTypedInput<TKey> target)
            where TKey : class
            => new TaggedComputedRef(target.Function, target.Input, target.Tag);

        // Internal methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Strip<T>(this IComputed<T>? computed) 
            => computed != null ? computed.Value : default!;
    }
}
