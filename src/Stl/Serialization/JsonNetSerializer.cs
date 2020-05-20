using Newtonsoft.Json;

namespace Stl.Serialization
{
    public class JsonNetSerializer<TNative> : ISerializer<TNative, string>
    {
        protected JsonSerializerSettings SerializerSettings { get; }

        public JsonNetSerializer(JsonSerializerSettings? serializerSettings = null)
        {
            serializerSettings ??= new JsonSerializerSettings() {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.All,
            };
            SerializerSettings = serializerSettings;
        }

        public string Serialize(TNative native) 
            // ReSharper disable once HeapView.BoxingAllocation
            => JsonConvert.SerializeObject(native, SerializerSettings);

        public TNative Deserialize(string serialized) 
            => JsonConvert.DeserializeObject<TNative>(serialized, SerializerSettings)!;
    }
}
