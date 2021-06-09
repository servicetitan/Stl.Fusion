using System;
using Newtonsoft.Json;
using Stl.Internal;
using Stl.Serialization;
using Stl.Serialization.Internal;

namespace Stl.Testing
{
    public static class SerializationTestEx
    {
        public static JsonSerializerSettings JsonSerializerSettings = new() {
            SerializationBinder = CrossPlatformSerializationBinder.Instance,
            Formatting = Formatting.Indented,
//            ContractResolver = new PreferSerializableContractResolver(),
        };

        public static T PassThroughAllSerializers<T>(this T value)
        {
            var v = value.PassThroughJsonConvert();
            v = v.PassThroughJsonSerialized();
            return v;
        }

        public static (T, string[]) PassThroughAllSerializersWithOutput<T>(this T value)
        {
            var (v1, json1) = value.PassThroughJsonConvertWithOutput();
            var (v2, json2) = v1.PassThroughJsonSerializedWithOutput();
            return (v2, new [] {json1, json2});
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

        public static T PassThroughJsonSerialized<T>(this T value)
        {
            var v1 = JsonSerialized.New(value);
            var v2 = JsonSerialized.New<T>(v1.Data);
            return v2.Value;
        }

        public static (T, string) PassThroughJsonSerializedWithOutput<T>(this T value)
        {
            var v1 = JsonSerialized.New(value);
            var v2 = JsonSerialized.New<T>(v1.Data);
            return (v2.Value, v1.Data);
        }
    }
}
