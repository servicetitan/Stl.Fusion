using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Stl.ImmutableModel.Internal;

namespace Stl.ImmutableModel
{
    public interface INode
    {
        Key Key { get; }
        DomainKey DomainKey { get; }
    }

    [Serializable]
    [JsonConverter(typeof(NodeJsonConverter))]
    public abstract class NodeBase: INode, ISerializable
    {
        public Key Key { get; protected set; }
        public virtual DomainKey DomainKey => (GetType(), Key);

        protected NodeBase(Key key) => Key = key;

        public override string ToString() => $"{GetType()}({Key})";
        
        // Serialization

        protected NodeBase(SerializationInfo info, StreamingContext context)
            : base()
        {
            Key = new Key(info.GetString(nameof(Key)) ?? "");
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) 
            => GetObjectData(info, context);
        protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Key), Key.Value);
        }
    }
}
