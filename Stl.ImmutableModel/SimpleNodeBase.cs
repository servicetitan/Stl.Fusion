using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stl.Collections;
using Stl.ImmutableModel.Reflection;

namespace Stl.ImmutableModel 
{
    [Serializable]
    public abstract class SimpleNodeBase : NodeBase, ISimpleNode 
    {
        internal static NodeTypeInfo CreateNodeTypeInfo(Type type) => new SimpleNodeTypeInfo(type);

        private Dictionary<Symbol, object>? _options;
        private Dictionary<Symbol, object> Options => _options ??= new Dictionary<Symbol, object>?();

        public override void Freeze()
        {
            if (IsFrozen) return;

            // First we freeze child freezables
            using var lease = ListBuffer<IFreezable>.Rent();
            var children = lease.Buffer;
            this.GetNodeType().FindChildFreezables(this, children);
            foreach (var child in children)
                child.Freeze();
            
            // And freeze itself in the end
            base.Freeze();
        }

        // IHasOptions implementation

        IEnumerator IEnumerable.GetEnumerator() 
            => ((IEnumerator?) _options?.GetEnumerator()) 
                ?? Enumerable.Empty<object>().GetEnumerator();
        IEnumerator<KeyValuePair<Symbol, object>> IEnumerable<KeyValuePair<Symbol, object>>.GetEnumerator() 
            => ((IEnumerator<KeyValuePair<Symbol, object>>?) _options?.GetEnumerator()) 
                ?? Enumerable.Empty<KeyValuePair<Symbol, object>>().GetEnumerator();

        public bool HasOption(Symbol key) => _options?.ContainsKey(key) ?? false;
        public object? GetOption(Symbol key) => _options?.GetValueOrDefault(key);
        
        public void SetOption(Symbol key, object? value)
        {
            if (value == null) {
                this.ThrowIfFrozen();
                _options?.Remove(key);
            }
            else {
                Options[key] = PrepareValue(key, value);
            }
        }
    }
}
