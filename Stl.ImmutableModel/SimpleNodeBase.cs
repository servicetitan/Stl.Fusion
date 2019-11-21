using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Stl.Collections;
using Stl.ImmutableModel.Reflection;
using Stl.Text;

namespace Stl.ImmutableModel 
{
    [JsonObject]
    public abstract class SimpleNodeBase : NodeBase, ISimpleNode 
    {
        internal static NodeTypeDef CreateNodeTypeDef(Type type) => new SimpleNodeTypeDef(type);

        [JsonProperty(
            PropertyName = "@Options", 
            DefaultValueHandling = DefaultValueHandling.Ignore)]
        private Dictionary<Symbol, object>? _options;
        private Dictionary<Symbol, object> Options => _options ??= new Dictionary<Symbol, object>();

        // IFreezable implementation

        public override void Freeze()
        {
            if (IsFrozen) return;

            // First we freeze child freezables
            var buffer = ListBuffer<KeyValuePair<Symbol, IFreezable>>.Lease();
            try {
                this.GetDefinition().GetFreezableItems(this, ref buffer);
                foreach (var (key, freezable) in buffer)
                    freezable.Freeze();
            }
            finally {
                buffer.Release();
            }
            
            // And freeze itself in the end
            base.Freeze();
        }

        public override IFreezable BaseToUnfrozen(bool deep = false)
        {
            var clone = (SimpleNodeBase) base.BaseToUnfrozen(deep);
            var nodeTypeDef = clone.GetDefinition();

            if (deep) {
                // Defrost every freezable
                var buffer = ListBuffer<KeyValuePair<Symbol, IFreezable>>.Lease();
                try {
                    nodeTypeDef.GetFreezableItems(clone, ref buffer);
                    foreach (var (key, f) in buffer)
                        nodeTypeDef.SetItem(clone, key, (object?) f.ToUnfrozen(true));
                }
                finally {
                    buffer.Release();
                }
            }
            else {
                // Defrost every collection (for convenience)
                var buffer = ListBuffer<KeyValuePair<Symbol, ICollectionNode>>.Lease();
                try {
                    nodeTypeDef.GetCollectionNodeItems(clone, ref buffer);
                    foreach (var (key, c) in buffer)
                        nodeTypeDef.SetItem(clone, key, (object?) c.ToUnfrozen());
                }
                finally {
                    buffer.Release();
                }
            }

            return clone;
        }

        // IHasOptions implementation

        public IEnumerable<KeyValuePair<Symbol, object>> GetAllOptions() 
            => _options ?? Enumerable.Empty<KeyValuePair<Symbol, object>>();

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
