using Newtonsoft.Json;

namespace Stl.Serialization
{
    public interface ISerializer<TNative, TSerialized>
    {
        TSerialized Serialize(TNative native);
        TNative Deserialize(TSerialized serialized);
    }

    public class PassThroughSerializer<TNative, TSerialized> : ISerializer<TNative, TSerialized>
    {
        // ReSharper disable once HeapView.BoxingAllocation
        public TSerialized Serialize(TNative native) => (TSerialized) (object) native!;
        // ReSharper disable once HeapView.BoxingAllocation
        public TNative Deserialize(TSerialized serialized) => (TNative) (object) serialized!;
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
