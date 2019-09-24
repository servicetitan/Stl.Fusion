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
        void SetOption<TValue>(object key, TValue value);
        [return: MaybeNull] TValue GetOption<TValue>(object key);
        bool HasOption(object key);
        void LockOptions();
    }

    public abstract class HasOptionsBase : IHasOptions
    {
        protected IDictionary<object, object> Options { get; private set; } = new Dictionary<object, object>();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<KeyValuePair<object, object>> GetEnumerator() => Options.GetEnumerator();

        void IHasOptions.SetOption(object key, object? value) => SetOptionUntyped(key, value);
        protected void SetOptionUntyped(object key, object? value)
        {
            if (value == null)
                Options.Remove(key);
            else 
                Options[key] = value;
        }

        object? IHasOptions.GetOption(object key) => GetOptionUntyped(key);
        protected object? GetOptionUntyped(object key) => Options.TryGetValue(key, out var value) ? value : null;

        void IHasOptions.SetOption<TValue>(object key, TValue value) => SetOption(key, value);
        protected void SetOption<TValue>(object key, TValue value) => SetOptionUntyped(key, value);

        [return: MaybeNull] 
        TValue IHasOptions.GetOption<TValue>(object key) => GetOption<TValue>(key);
        [return: MaybeNull] 
        protected TValue GetOption<TValue>(object key) => (TValue) (GetOptionUntyped(key) ?? default(TValue)!);

        bool IHasOptions.HasOption(object key) => HasOption(key);
        protected bool HasOption(object key) => Options.ContainsKey(key);

        void IHasOptions.LockOptions() => LockOptions();
        protected void LockOptions() => Options = new ReadOnlyDictionary<object, object>(Options); 
    }
}
