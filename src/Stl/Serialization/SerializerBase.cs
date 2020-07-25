using System;

namespace Stl.Serialization
{
    public abstract class SerializerBase<TSerialized> : ISerializer<TSerialized>
    {
        public TSerialized Serialize<T>(T native)
            // ReSharper disable once HeapView.BoxingAllocation
            => Serialize(native, typeof(T));

        public abstract TSerialized Serialize(object? native, Type type);

        public T Deserialize<T>(TSerialized serialized)
            => (T) Deserialize(serialized, typeof(T))!;

        public abstract object? Deserialize(TSerialized serialized, Type type);

        public TypedSerializer<T, TSerialized> ToTyped<T>()
            => new TypedSerializer<T, TSerialized>(this);
    }
}
