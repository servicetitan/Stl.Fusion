using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Stl.Text;

namespace Stl.Extensibility
{
    public interface IHasOptions
    {
        // Shouldn't store any options with null values; passing null = removing the option
        IEnumerable<KeyValuePair<Symbol, object>> GetAllOptions();
        bool HasOption(Symbol key);
        object? GetOption(Symbol key);
        void SetOption(Symbol key, object? value);
    }

    [Serializable]
    public abstract class HasOptionsBase : IHasOptions
    {
        [JsonProperty]
        protected IDictionary<Symbol, object> Options { get; private set; } =
            new Dictionary<Symbol, object>();

        public IEnumerable<KeyValuePair<Symbol, object>> GetAllOptions() => Options;

        // The intent is to keep these methods exposed only via IHasOptions
        bool IHasOptions.HasOption(Symbol key) => HasOption(key);
        protected bool HasOption(Symbol key) => Options.ContainsKey(key);

        object? IHasOptions.GetOption(Symbol key) => GetOption(key);
        protected object? GetOption(Symbol key)
            => Options.TryGetValue(key, out var value) ? value : null;

        void IHasOptions.SetOption(Symbol key, object? value) => SetOption(key, value);
        protected void SetOption(Symbol key, object? value)
        {
            if (value == null)
                Options.Remove(key);
            else
                Options[key] = value;
        }
    }
}
