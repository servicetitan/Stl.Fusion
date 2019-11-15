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
        internal static NodeTypeDef CreateNodeTypeInfo(Type type) => new SimpleNodeTypeDef(type);

        private Dictionary<Symbol, object>? _options;
        private Dictionary<Symbol, object> Options => _options ??= new Dictionary<Symbol, object>();

        // IFreezable implementation

        public override void Freeze()
        {
            if (IsFrozen) return;

            // First we freeze child freezables
            using var lease = ListBuffer<KeyValuePair<Symbol, IFreezable>>.Rent();
            var buffer = lease.Buffer;
            this.GetDefinition().GetFreezableItems(this, buffer);
            foreach (var (key, freezable) in buffer)
                freezable.Freeze();
            
            // And freeze itself in the end
            base.Freeze();
        }

        public override IFreezable BaseDefrost(bool deep = false)
        {
            var clone = (SimpleNodeBase) base.BaseDefrost(deep);
            var nodeTypeDef = clone.GetDefinition();

            if (deep) {
                // Defrost every freezable
                using var lease = ListBuffer<KeyValuePair<Symbol, IFreezable>>.Rent();
                var buffer = lease.Buffer;
                nodeTypeDef.GetFreezableItems(clone, buffer);
                foreach (var (key, f) in buffer)
                    nodeTypeDef.SetItem(clone, key, f.Defrost());
            }
            else {
                // Defrost every collection (for convenience)
                using var lease = ListBuffer<KeyValuePair<Symbol, ICollectionNode>>.Rent();
                var buffer = lease.Buffer;
                nodeTypeDef.GetCollectionNodeItems(clone, buffer);
                foreach (var (key, c) in buffer)
                    nodeTypeDef.SetItem(clone, key, c.Defrost());
            }

            return clone;
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
            key.ThrowIfInvalidOptionsKey();
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
