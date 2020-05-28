namespace Stl.Serialization
{
    public class PassThroughSerializer<T, TSerialized> : ISerializer<T, TSerialized>
    {
        // ReSharper disable once HeapView.BoxingAllocation
        public TSerialized Serialize(T native) => (TSerialized) (object) native!;
        // ReSharper disable once HeapView.BoxingAllocation
        public T Deserialize(TSerialized serialized) => (T) (object) serialized!;
    }

    public class PassThroughSerializer<TNative> : ISerializer<TNative, object>
    {
        // ReSharper disable once HeapView.BoxingAllocation
        public object Serialize(TNative native) => native!;
        public TNative Deserialize(object serialized) => (TNative) serialized;
    }
}
