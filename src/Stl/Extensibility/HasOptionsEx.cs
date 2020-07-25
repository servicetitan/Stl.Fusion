using System;
using System.Diagnostics.CodeAnalysis;
using Stl.Reflection;
using Stl.Text;

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

        public static bool HasOption(this IHasOptions hasOptions, Type type)
            => hasOptions.HasOption(type.ToSymbol());
        public static bool HasOption<TValue>(this IHasOptions hasOptions)
            => hasOptions.HasOption(typeof(TValue));

        [return: MaybeNull]
        public static object? GetOption(this IHasOptions hasOptions, Type type)
            => hasOptions.GetOption(type.ToSymbol());
        [return: MaybeNull]
        public static TValue GetOption<TValue>(this IHasOptions hasOptions)
            => (TValue) (hasOptions.GetOption(typeof(TValue)) ?? default!);

        public static void SetOption(this IHasOptions hasOptions, Type type, object? value)
            => hasOptions.SetOption(type.ToSymbol(), value);
        public static void SetOption<TValue>(this IHasOptions hasOptions, TValue value)
            => hasOptions.SetOption(typeof(TValue), value);

        public static T WithOption<T>(this T hasOptions, Type type, object? value)
            where T : IHasOptions
            => hasOptions.WithOption(type.ToSymbol(), value);
        public static T WithOption<T, TValue>(this T hasOptions, TValue value)
            where T : IHasOptions
            => hasOptions.WithOption(typeof(TValue), value);
    }
}
