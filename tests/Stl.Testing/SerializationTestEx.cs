using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using Stl.Internal;
using Stl.Serialization;

namespace Stl.Testing
{
    public static class SerializationTestEx
    {
        public static JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings() {
            Formatting = Formatting.Indented,
//            ContractResolver = new PreferSerializableContractResolver(),
        }; 

        public static T PassThroughAllSerializers<T>(this T value)
        {
            var v = value.PassThroughJsonConvert();
            v = v.PassThroughBinaryFormatter();
            return v;
        }

        public static (T, string) PassThroughAllSerializersWithOutput<T>(this T value)
        {
            var (v, json) = value.PassThroughJsonConvertWithOutput(); 
            v = v.PassThroughBinaryFormatter();
            return (v, json);
        }

        public static T PassThroughJsonConvert<T>(this T value)
        {
            var box = Box.New(value);
            var json = JsonConvert.SerializeObject(box, JsonSerializerSettings);
            box = JsonConvert.DeserializeObject<Box<T>>(json, JsonSerializerSettings)!;
            return box.Value;
        }
        
        public static (T, string) PassThroughJsonConvertWithOutput<T>(this T value)
        {
            var box = Box.New(value);
            var json = JsonConvert.SerializeObject(box, JsonSerializerSettings);
            box = JsonConvert.DeserializeObject<Box<T>>(json, JsonSerializerSettings)!;
            return (box.Value, json);
        }
        
        public static T PassThroughBinaryFormatter<T>(this T value)
        {
            var box = Box.New(value);
            var ms = new MemoryStream();
            var bf = new BinaryFormatter();
            bf.Serialize(ms, box);
            ms.Seek(0, SeekOrigin.Begin);
            box = (Box<T>) bf.Deserialize(ms);
            return box.Value;
        }
    }
}
