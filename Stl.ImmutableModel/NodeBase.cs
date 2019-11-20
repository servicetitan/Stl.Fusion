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
    [JsonObject]
    public abstract class NodeBase: FreezableBase, INode
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
