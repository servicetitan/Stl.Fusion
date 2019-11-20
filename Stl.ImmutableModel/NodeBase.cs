using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stl.Collections;
using Stl.ImmutableModel.Indexing;

namespace Stl.ImmutableModel
{
    [Serializable]
    public abstract class NodeBase: FreezableBase, INode, ISerializable
    {
        private Key _key;

        public Key Key {
            get => _key;
            set {
                this.ThrowIfFrozen(); 
                _key = value;
            }
        }

        [JsonIgnore]
        public Symbol LocalKey => Key.Parts.Tail;

        public override string ToString() => $"{GetType().Name}({Key})";

        protected T PrepareValue<T>(Symbol localKey, T value)
        {
            this.ThrowIfFrozen();
            if (value is INode node && node.Key.IsUndefined()) {
                // We automatically provide keys for INode properties (or collection items)
                // by extending the owner's key with property name suffix 
                node.Key = Key + localKey;
            }
            return value;
        }

        // ISerializable

        protected NodeBase() { }
        protected NodeBase(SerializationInfo info, StreamingContext context)
        {
            var nodeTypeDef = this.GetDefinition();
            Key = Key.Parse(info.GetString(nameof(Key))!);
            foreach (var entry in info) {
                if (entry.Name == nameof(Key))
                    continue;
                if (entry.Value is JObject jObject) {
                    var value = jObject.ToObject<object>();
                    nodeTypeDef.SetItem(this, (Symbol) entry.Name, value);
                    continue;
                }
                nodeTypeDef.SetItem(this, (Symbol) entry.Name, entry.Value);
            }
        }
        
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            var nodeTypeDef = this.GetDefinition();
            info.AddValue(nameof(Key), Key.FormattedValue);
            foreach (var (key, value) in nodeTypeDef.GetAllItems(this)) 
                info.AddValue(key.Value, value);
        }
        // IFreezable

        public override void Freeze()
        {
            Key.ThrowIfUndefined(); 
            base.Freeze();
        }

        // IHasChangeHistory

        (object? BaseState, object? CurrentState, IEnumerable<(Symbol LocalKey, DictionaryEntryChangeType ChangeType, object? Value)> Changes) 
            IHasChangeHistory.GetChangeHistory() 
            => GetChangeHistoryUntyped();
        protected virtual (object? BaseState, object? CurrentState, IEnumerable<(Symbol LocalKey, DictionaryEntryChangeType ChangeType, object? Value)> Changes) GetChangeHistoryUntyped()
            => (null, null, Enumerable.Empty<(Symbol LocalKey, DictionaryEntryChangeType ChangeType, object? Value)>());

        void IHasChangeHistory.DiscardChangeHistory() => DiscardChangeHistory();
        protected virtual void DiscardChangeHistory() {}
    }
}
