using Stl.Serialization;

namespace Stl.Testing
{
    public class PassThroughSerializer<T, TSerialized> : ITypedSerializer<T, TSerialized>
    {
        // ReSharper disable once HeapView.BoxingAllocation
        public TSerialized Serialize(T native) => (TSerialized) (object) native!;
        // ReSharper disable once HeapView.BoxingAllocation
        public T Deserialize(TSerialized serialized) => (T) (object) serialized!;
    }

    public class PassThroughSerializer<TNative> : ITypedSerializer<TNative, object>
    {
        // ReSharper disable once HeapView.BoxingAllocation
        public object Serialize(TNative native) => native!;
        public TNative Deserialize(object serialized) => (TNative) serialized;
    }
}
