using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Stl.Extensibility
{
    public interface IHasOptions : IEnumerable<KeyValuePair<object, object>>
    {
        // Shouldn't store any options with null values
        void SetOption(object key, object? value);
        object? GetOption(object key);
        bool HasOption(object key);
        void LockOptions();
    }

    public static class HasOptionsEx
    {
        [return: MaybeNull]
        public static TValue GetOption<TValue>(this IHasOptions hasOptions, object key) 
            // ReSharper disable once HeapView.BoxingAllocation
            => (TValue) (hasOptions.GetOption(key) ?? default(TValue)!);
        public static void SetOption<TValue>(this IHasOptions hasOptions, object key, TValue value) 
            => hasOptions.SetOption(key, value);
    }

    public abstract class HasOptionsBase : IHasOptions
    {
        protected IDictionary<object, object> Options { get; private set; } = 
            new Dictionary<object, object>();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<KeyValuePair<object, object>> GetEnumerator() => Options.GetEnumerator();

        // The intent is to keep these methods exposed only via IHasOptions
        bool IHasOptions.HasOption(object key) => HasOption(key);
        object? IHasOptions.GetOption(object key) => GetOption(key); 
        void IHasOptions.SetOption(object key, object? value) => SetOption(key, value); 
        void IHasOptions.LockOptions() => LockOptions();

        // And via protected members
        protected bool HasOption(object key) => Options.ContainsKey(key);
        protected void SetOption(object key, object? value)
        {
            if (value == null)
                Options.Remove(key);
            else 
                Options[key] = value;
        }
        protected object? GetOption(object key) 
            => Options.TryGetValue(key, out var value) ? value : null;
        protected void LockOptions() => Options = new ReadOnlyDictionary<object, object>(Options); 
    }
}
