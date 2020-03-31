using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Stl.Collections;
using Stl.ImmutableModel.Indexing;
using Stl.ImmutableModel.Reflection;
using Stl.Text;

namespace Stl.ImmutableModel
{
    [JsonObject]
    public class Node: FreezableBase, INode
    {
        internal static NodeTypeDef CreateNodeTypeDef(Type type) => new NodeTypeDef(type);

        private Key _key = null!;

        [JsonProperty(
            PropertyName = "@Options", 
            DefaultValueHandling = DefaultValueHandling.Ignore)]
        private Dictionary<Symbol, object>? _options;
        private Dictionary<Symbol, object> Options => _options ??= new Dictionary<Symbol, object>();

        public Key Key {
            get => _key;
            set {
                this.ThrowIfFrozen(); 
                _key = value;
            }
        }

        public Node() { }
        public Node(Key key) => Key = key;

        public override string ToString() => $"{GetType().Name}({Key})";

        // IFreezable implementation

        public override void Freeze()
        {
            if (IsFrozen) return;
            Key.ThrowIfNull(); 

            // First we freeze child freezables
            var buffer = ListBuffer<KeyValuePair<ItemKey, IFreezable>>.Lease();
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
            var clone = (Node) base.BaseToUnfrozen(deep);
            var nodeTypeDef = clone.GetDefinition();

            if (deep) {
                // Defrost every freezable
                var buffer = ListBuffer<KeyValuePair<ItemKey, IFreezable>>.Lease();
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
                var buffer = ListBuffer<KeyValuePair<ItemKey, ICollectionNode>>.Lease();
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
                Options[key] = PrepareOptionValue(key, value);
            }
        }

        // IHasChangeHistory

        (object? BaseState, object? CurrentState, IEnumerable<(Key Key, DictionaryEntryChangeType ChangeType, object? Value)> Changes) 
            IHasChangeHistory.GetChangeHistory() 
            => GetChangeHistoryUntyped();
        protected virtual (object? BaseState, object? CurrentState, IEnumerable<(Key Key, DictionaryEntryChangeType ChangeType, object? Value)> Changes) GetChangeHistoryUntyped()
            => (null, null, Enumerable.Empty<(Key Key, DictionaryEntryChangeType ChangeType, object? Value)>());

        void IHasChangeHistory.DiscardChangeHistory() => DiscardChangeHistory();
        protected virtual void DiscardChangeHistory() {}

        // Protected & private members

        protected T PreparePropertyValue<T>(Symbol propertyName, T value)
        {
            this.ThrowIfFrozen();
            if (value is INode node && node.Key.IsNull()) {
                // We automatically provide keys for INode properties (or collection items)
                // by extending the owner's key with property name suffix 
                node.Key = new PropertyKey(propertyName, Key);
            }
            return value;
        }

        protected T PrepareOptionValue<T>(Symbol optionName, T value)
        {
            this.ThrowIfFrozen();
            if (value is INode node && node.Key.IsNull()) {
                // We automatically provide keys for INode properties (or collection items)
                // by extending the owner's key with property name suffix 
                node.Key = new OptionKey(optionName, Key);
            }
            return value;
        }
    }
}
