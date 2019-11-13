using System;

namespace Stl.ImmutableModel
{
    [Serializable]
    public abstract class NodeBase: FreezableBase, INode
    {
        public virtual Key Key { get; }
        public Symbol LocalKey => Key.Parts.Tail;

        protected NodeBase(Key key) => Key = key;

        public override string ToString() => $"{GetType().Name}({Key})";
    }
}
