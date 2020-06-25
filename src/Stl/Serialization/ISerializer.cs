using System;

namespace Stl.Serialization
{
    public interface ISerializer<TSerialized>
    {
        TSerialized Serialize<T>(T native);
        TSerialized Serialize(object? native, Type type);
        T Deserialize<T>(TSerialized serialized);
        object? Deserialize(TSerialized serialized, Type type);

        TypedSerializer<T, TSerialized> ToTyped<T>();
    }
}
