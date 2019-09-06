using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using Stl.Internal;

namespace Stl.Testing
{
    public static class SerializationTestEx
    {
        public static T PassThroughJsonConvert<T>(this T value)
        {
            var box = Box.New(value);
            var json = JsonConvert.SerializeObject(box);
            box = JsonConvert.DeserializeObject<Box<T>>(json);
            return box.Value;
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