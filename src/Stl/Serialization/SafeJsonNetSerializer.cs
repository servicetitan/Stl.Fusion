using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Stl.Internal;
using Stl.OS;
using Stl.Reflection;
using Stl.Text;

namespace Stl.Serialization
{
    public class SafeJsonNetSerializer<TNative> : ISerializer<TNative, string>
    {
        protected static readonly Func<TypeRef, bool> DefaultVerifier = 
            typeRef => typeof(TNative).IsAssignableFrom(typeRef.Resolve()); 

        protected Func<TypeRef, bool> Verifier { get; set; }
        protected JsonSerializerSettings SerializerSettings { get; set; }

        public SafeJsonNetSerializer(Func<TypeRef, bool>? verifier = null)
        {
            Verifier = verifier ?? DefaultVerifier;
            SerializerSettings = new JsonSerializerSettings() {
                TypeNameHandling = TypeNameHandling.None,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            };
        }

        public string Serialize(TNative native) 
        {
            var typeRef = new TypeRef(native!.GetType());
            if (!Verifier.Invoke(typeRef))
                throw Errors.UnsupportedTypeForJsonSerialization(typeRef);
            var json = JsonConvert.SerializeObject(native, typeRef.Resolve(), SerializerSettings);

            var sb = new StringBuilder();
            var f = ListFormat.Default.CreateFormatter(sb);
            f.Append(NormalizeTypeName(typeRef.AssemblyQualifiedName.Value));
            f.Append(json);
            f.AppendEnd();
            return sb.ToString();
        }

        public TNative Deserialize(string serialized)
        {
            var sb = new StringBuilder();
            var p = ListFormat.Default.CreateParser(serialized, sb);
            
            p.ParseNext();
            var typeRef = new TypeRef(DenormalizeTypeName(p.Item));
            if (!Verifier.Invoke(typeRef))
                throw Errors.UnsupportedTypeForJsonSerialization(typeRef);

            p.ParseNext();
            var result = JsonConvert.DeserializeObject(p.Item, typeRef.Resolve(), SerializerSettings);
            return (TNative) result!;
        }

        private static string NormalizeTypeName(string value)
        {
            value = value.Replace(", mscorlib, Version=2.0.5.0,", ", <mscorlib>,");
            value = value.Replace(", System.Private.CoreLib, Version=4.0.0.0,", ", <mscorlib>,");
            return value;
        }

        private static string DenormalizeTypeName(string value) 
            => value.Replace(", <mscorlib>,", 
                OSInfo.Kind == OSKind.Wasm 
                    ? ", mscorlib, Version=2.0.5.0," : 
                    ", System.Private.CoreLib, Version=4.0.0.0,");
    }
}
