using System.Collections.Generic;
using System.Collections.ObjectModel;
using Stl.Extensibility;
using Stl.Text;

namespace Stl.Frozen
{
    public interface IFrozenHasOptions : IHasOptions, IFrozen { }

    public abstract class FrozenHasOptionsBase : FrozenBase, IFrozenHasOptions
    {
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
            this.ThrowIfFrozen();
            if (value == null)
                Options.Remove(key);
            else 
                Options[key] = value;
        }

        // IFrozen-related

        public override void Freeze()
        {
            if (IsFrozen) return;
            foreach (var optionValue in Options.Values)
                if (optionValue is IFrozen f)
                    f.Freeze();
            Options = new ReadOnlyDictionary<Symbol, object>(Options);
            base.Freeze();
        }

        public override IFrozen CloneToUnfrozenUntyped(bool deep = false)
        {
            var clone = (FrozenHasOptionsBase) base.CloneToUnfrozenUntyped(deep);
            var options = new Dictionary<Symbol, object>(Options.Count);
            foreach (var (key, option) in Options) {
                if (option is IFrozen f)
                    options.Add(key, f.ToUnfrozen(deep));
                else
                    options.Add(key, option);
            }
            clone.Options = options;
            return clone;
        }
    }
}
