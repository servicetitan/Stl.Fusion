using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Stl.Purifier
{
    public static class ComputedEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNull<T>([NotNullWhen(false)] this T? computed)
            where T : class, IComputed
            => ReferenceEquals(computed, null);

        public static ComputedRef<TKey> ToRef<TKey>(
            this IComputedWithTypedInput<TKey> target)
            where TKey : notnull
            => new ComputedRef<TKey>(target.Function, target.Input, target.Tag);
    }
}
