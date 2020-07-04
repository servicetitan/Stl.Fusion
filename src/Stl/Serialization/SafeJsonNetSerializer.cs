using System;
using System.Text;
using Newtonsoft.Json.Serialization;
using Stl.Internal;
using Stl.Reflection;
using Stl.Text;

namespace Stl.Serialization
{
    public class SafeJsonNetSerializer : SerializerBase<string>
    {
        private readonly ISerializationBinder _serializationBinder;
        private readonly StringBuilder _stringBuilder;

        public Func<Type, bool> Verifier { get; }
        public JsonNetSerializer UnsafeSerializer { get; }

        public SafeJsonNetSerializer(Func<Type, bool> verifier) 
            : this(new JsonNetSerializer(), verifier) { }
        public SafeJsonNetSerializer(JsonNetSerializer unsafeSerializer, Func<Type, bool> verifier)
        {
            UnsafeSerializer = unsafeSerializer;
            Verifier = verifier;

            _serializationBinder = UnsafeSerializer.Settings.SerializationBinder ?? CrossPlatformSerializationBinder.Instance;
            _stringBuilder = new StringBuilder(256);
        }

        public override string Serialize(object? native, Type type)
        {
            _stringBuilder.Clear();
            var f = ListFormat.Default.CreateFormatter(_stringBuilder);
            if (native == null) {
                // Special case: null serialization
                f.Append("");
                f.AppendEnd();
            }
            else {
                var actualType = native.GetType();
                if (!Verifier.Invoke(actualType))
                    throw Errors.UnsupportedTypeForJsonSerialization(type);
                var aqn = actualType.GetAssemblyQualifiedName(false, _serializationBinder);
                var json = UnsafeSerializer.Serialize(native);
                f.Append(aqn);
                f.Append(json);
                f.AppendEnd();
            }
            return _stringBuilder.ToString();
        }

        public override object? Deserialize(string serialized, Type type) 
        {
            _stringBuilder.Clear();
            var p = ListFormat.Default.CreateParser(serialized, _stringBuilder);
            
            p.ParseNext();
            if (string.IsNullOrEmpty(p.Item))
                // Special case: null deserialization
                return null;

            TypeNameHelpers.SplitAssemblyQualifiedName(p.Item, out var assemblyName, out var typeName);
            var actualType = _serializationBinder.BindToType(assemblyName, typeName);
            if (!type.IsAssignableFrom(actualType))
                throw Errors.UnsupportedTypeForJsonSerialization(actualType);
            if (!Verifier.Invoke(actualType))
                throw Errors.UnsupportedTypeForJsonSerialization(actualType);

            p.ParseNext();
            return UnsafeSerializer.Deserialize(p.Item, actualType);
        }
    }
}
