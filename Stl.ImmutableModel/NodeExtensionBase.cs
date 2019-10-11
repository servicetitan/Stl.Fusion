using System;
using System.Runtime.Serialization;

namespace Stl.ImmutableModel 
{
    [Serializable]
    public abstract class ExtensionNodeBase<TSelf, TTarget> : SimpleNodeBase, IExtensionNode
        where TSelf : ExtensionNodeBase<TSelf, TTarget>
        where TTarget : class, ISimpleNode
    {
        protected ExtensionNodeBase(TTarget target) : this(target.Key) { }
        protected ExtensionNodeBase(Key targetKey) : base(targetKey + ExtendableNodeEx.GetExtensionKey(typeof(TSelf))) { }
        protected ExtensionNodeBase(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
