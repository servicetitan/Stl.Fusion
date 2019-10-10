using System;
using System.Runtime.Serialization;

namespace Stl.ImmutableModel 
{
    [Serializable]
    public abstract class ExtensionNodeBase<TExt, TTarget> : SimpleNodeBase, IExtensionNode
        where TExt : ExtensionNodeBase<TExt, TTarget>
        where TTarget : class, ISimpleNode
    {
        protected ExtensionNodeBase(Key key) : base(key) { }
        protected ExtensionNodeBase(TTarget target) : base(target.Key + ExtendableNodeEx.GetExtensionKey(typeof(TExt))) { }
        protected ExtensionNodeBase(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
