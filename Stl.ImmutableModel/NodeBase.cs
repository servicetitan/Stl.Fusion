using System;
using Stl.ImmutableModel.Internal;

namespace Stl.ImmutableModel
{
    [Serializable]
    public abstract class NodeBase: FreezableBase, INode
    {
        private Key? _key;

        public Key Key {
            get => _key ?? throw Errors.KeyIsNotSetYet();
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
    }
}
