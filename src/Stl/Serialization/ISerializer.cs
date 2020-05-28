namespace Stl.Serialization
{
    public interface ISerializer<T, TSerialized>
    {
        TSerialized Serialize(T native);
        T Deserialize(TSerialized serialized);
    }
}
