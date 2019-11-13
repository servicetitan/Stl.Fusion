using System.Diagnostics.CodeAnalysis;
using Stl.Reflection;

namespace Stl.Extensibility 
{
    public static class HasOptionsEx
    {
        // Generic overloads
        
        [return: MaybeNull]
        public static TValue GetOption<TValue>(this IHasOptions hasOptions, Symbol key) 
            // ReSharper disable once HeapView.BoxingAllocation
            => (TValue) (hasOptions.GetOption(key) ?? default(TValue)!);

        public static T WithOption<T>(this T hasOptions, Symbol key, object? value)
            where T : IHasOptions
        {
            hasOptions.SetOption(key, value);
            return hasOptions;
        }

        // Type-keyed overloads

        public static bool HasOption<TValue>(this IHasOptions hasOptions)
            => hasOptions.HasOption(typeof(TValue).ToSymbol());

        [return: MaybeNull]
        public static TValue GetOption<TValue>(this IHasOptions hasOptions)
            => hasOptions.GetOption<TValue>(typeof(TValue).ToSymbol());

        public static void SetOption<TValue>(this IHasOptions hasOptions, TValue value)
            => hasOptions.SetOption(typeof(TValue).ToSymbol(), value);

        public static T WithOption<T, TValue>(this T hasOptions, TValue value)
            where T : IHasOptions
            => hasOptions.WithOption(typeof(TValue).ToSymbol(), value);
    }
}
