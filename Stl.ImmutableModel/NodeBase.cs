using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Stl.ImmutableModel.Internal;
using Stl.ImmutableModel.Reflection;

namespace Stl.ImmutableModel
{
    [Serializable]
    [JsonConverter(typeof(NodeJsonConverter))]
    public abstract class NodeBase: INode, ISerializable
    {
        public virtual Key Key { get; protected set; }
        public bool IsFrozen { get; protected set; }
        
        protected NodeBase(Key key) => Key = key;

        public override string ToString() => $"{GetType().Name}({Key})";

        // IFreezable
        
        public virtual void Freeze()
        {
            IsFrozen = true;
        }

        public virtual IFreezable BaseDefrost()
        {
            var clone = (NodeBase) MemberwiseClone();
            clone.IsFrozen = false;
            return clone;
        }

        // Serialization

        protected NodeBase(SerializationInfo info, StreamingContext context)
            : base()
        {
            Key = Key.Parse(info.GetString(nameof(Key)) ?? "");
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) 
            => GetObjectData(info, context);
        protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Key), Key.FormattedValue);
        }
    }
}
