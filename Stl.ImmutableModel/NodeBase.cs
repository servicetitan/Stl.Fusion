using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stl.Collections;
using Stl.ImmutableModel.Indexing;
using Stl.Text;

namespace Stl.ImmutableModel
{
    [JsonObject]
    public abstract class NodeBase: FreezableBase, INode
    {
        private Key _key = null!;

        public Key Key {
            get => _key;
            set {
                this.ThrowIfFrozen(); 
                _key = value;
            }
        }

        public override string ToString() => $"{GetType().Name}({Key})";

        // IFreezable

        public override void Freeze()
        {
            Key.ThrowIfUndefined(); 
            base.Freeze();
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
            if (value is INode node && node.Key.IsUndefined()) {
                // We automatically provide keys for INode properties (or collection items)
                // by extending the owner's key with property name suffix 
                node.Key = new PropertyKey(propertyName, Key);
            }
            return value;
        }

        protected T PrepareOptionValue<T>(Symbol optionName, T value)
        {
            this.ThrowIfFrozen();
            if (value is INode node && node.Key.IsUndefined()) {
                // We automatically provide keys for INode properties (or collection items)
                // by extending the owner's key with property name suffix 
                node.Key = new OptionKey(optionName, Key);
            }
            return value;
        }
    }
}
