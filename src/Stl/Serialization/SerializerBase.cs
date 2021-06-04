using System;

namespace Stl.Serialization
{
    public abstract class SerializerBase<TSerialized> : ISerializer<TSerialized>
    {
        public abstract TSerialized Serialize(object? native, Type? type);
        public abstract object? Deserialize(TSerialized serialized, Type? type);

        // ReSharper disable once HeapView.PossibleBoxingAllocation
        public TSerialized Serialize<T>(T native) => Serialize(native, typeof(T));
        public T Deserialize<T>(TSerialized serialized) => (T) Deserialize(serialized, typeof(T))!;

        public TypedSerializer<T, TSerialized> ToTyped<T>()
            => new(Serialize<T>, Deserialize<T>);
    }
}
