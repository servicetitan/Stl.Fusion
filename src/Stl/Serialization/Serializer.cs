using Newtonsoft.Json;

namespace Stl.Serialization
{
    public interface ISerializer<T, TSerialized>
    {
        TSerialized Serialize(T native);
        T Deserialize(TSerialized serialized);
    }

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

    public class JsonNetSerializer<TNative> : ISerializer<TNative, string>
    {
        public JsonSerializerSettings Settings { get; }

        public JsonNetSerializer(JsonSerializerSettings? settings = null)
        {
            settings ??= new JsonSerializerSettings() {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.All,
            };
            Settings = settings;
        }

        public string Serialize(TNative native) 
            // ReSharper disable once HeapView.BoxingAllocation
            => JsonConvert.SerializeObject(native, Settings);
        public TNative Deserialize(string serialized) 
            => JsonConvert.DeserializeObject<TNative>(serialized, Settings)!;
    }
}
