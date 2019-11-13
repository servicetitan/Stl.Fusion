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

        private Dictionary<Symbol, object>? _options = null;
        private Dictionary<Symbol, object> Options => 
            _options ??= new Dictionary<Symbol, object>?();

        protected SimpleNodeBase(Key key) : base(key) { }

        public override void Freeze()
        {
            if (IsFrozen) return;

            // Freezing child freezables
            using var lease = ZList<IFreezable>.Rent();
            var children = lease.List;
            this.GetNodeType().GetChildFreezables(this, children);
            foreach (var child in children)
                child.Freeze();
            
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
            this.ThrowIfFrozen();
            if (value == null)
                _options?.Remove(key);
            else
                Options[key] = value;
        }
    }
}
