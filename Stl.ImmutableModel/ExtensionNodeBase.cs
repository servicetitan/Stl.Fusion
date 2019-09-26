using System;
using System.Runtime.Serialization;

namespace Stl.ImmutableModel 
{
    public interface IExtensionNode : ISimpleNode { }

    [Serializable]
    public abstract class ExtensionNodeBase<TExt, TTarget> : SimpleNodeBase, IExtensionNode
        where TExt : ExtensionNodeBase<TExt, TTarget>
        where TTarget : class, ISimpleNode
    {
        protected ExtensionNodeBase(Key key) : base(key) { }
        protected ExtensionNodeBase(TTarget target) : base(target.Key + Ext.GetPropertyKey(typeof(TExt))) { }
        protected ExtensionNodeBase(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
