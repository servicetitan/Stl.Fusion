using System;
using System.Collections.Generic;
using System.Linq;
using Stl.Collections;
using Stl.ImmutableModel.Indexing;
using Stl.ImmutableModel.Internal;

namespace Stl.ImmutableModel
{
    [Serializable]
    public abstract class NodeBase: FreezableBase, INode
    {
        private Key? _key;

        public Key Key {
            get => _key ?? throw Errors.NodeHasNoKey();
            set {
                this.ThrowIfFrozen(); 
                _key = value;
            }
        }
        public bool HasKey => _key != null;
        public Symbol LocalKey => Key.Parts.Tail;

        public override string ToString() => $"{GetType().Name}({Key})";

        protected T PrepareValue<T>(Symbol localKey, T value)
        {
            this.ThrowIfFrozen();
            if (value is INode node && !node.HasKey) {
                // We automatically provide keys for INode properties (or collection items)
                // by extending the owner's key with property name suffix 
                node.Key = Key + localKey;
            }
            return value;
        }

        // IFreezable

        public override void Freeze()
        {
            if (!HasKey) throw Errors.NodeHasNoKey();
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
