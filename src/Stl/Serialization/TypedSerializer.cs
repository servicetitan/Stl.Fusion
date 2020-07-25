namespace Stl.Serialization
{
    public interface ITypedSerializer<T, TSerialized>
    {
        TSerialized Serialize(T native);
        T Deserialize(TSerialized serialized);
    }

    public sealed class TypedSerializer<T, TSerialized> : ITypedSerializer<T, TSerialized>
    {
        private readonly ISerializer<TSerialized> _serializer;

        public TypedSerializer(ISerializer<TSerialized> serializer)
            => _serializer = serializer;

        public TSerialized Serialize(T native)
            => _serializer.Serialize(native);

        public T Deserialize(TSerialized serialized)
            => _serializer.Deserialize<T>(serialized);
    }
}
