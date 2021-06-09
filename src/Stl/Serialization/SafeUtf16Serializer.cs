using System;
using System.Text;
using Newtonsoft.Json.Serialization;
using Stl.Internal;
using Stl.Reflection;
using Stl.Serialization.Internal;
using Stl.Text;

namespace Stl.Serialization
{
    public class SafeUtf16Serializer : Utf16SerializerBase
    {
        private readonly ISerializationBinder _serializationBinder;
        private readonly StringBuilder _stringBuilder;

        public Func<Type, bool> Verifier { get; }
        public IUtf16Serializer UnsafeSerializer { get; }

        public SafeUtf16Serializer(IUtf16Serializer unsafeSerializer, Func<Type, bool> verifier)
        {
            UnsafeSerializer = unsafeSerializer;
            Verifier = verifier;
            var serializationBinder = (UnsafeSerializer as NewtonsoftJsonSerializer)?.Settings?.SerializationBinder;
#if NET5
            serializationBinder ??= SerializationBinder.Instance;
#else
            serializationBinder ??= CrossPlatformSerializationBinder.Instance;
#endif
            _serializationBinder = serializationBinder;
            _stringBuilder = new StringBuilder(256);
        }

        public override object? Read(string data, Type type)
        {
            _stringBuilder.Clear();
            var p = ListFormat.Default.CreateParser(data, _stringBuilder);

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
            return UnsafeSerializer.Reader.Read(p.Item, actualType);
        }

        public override string Write(object? value, Type type)
        {
            _stringBuilder.Clear();
            var f = ListFormat.Default.CreateFormatter(_stringBuilder);
            if (value == null) {
                // Special case: null serialization
                f.Append("");
                f.AppendEnd();
            }
            else {
                var actualType = value.GetType();
                if (!Verifier.Invoke(actualType))
                    throw Errors.UnsupportedTypeForJsonSerialization(type);
                var aqn = actualType.GetAssemblyQualifiedName(false, _serializationBinder);
                var json = UnsafeSerializer.Writer.Write(value, type);
                f.Append(aqn);
                f.Append(json);
                f.AppendEnd();
            }
            return _stringBuilder.ToString();
        }
    }
}
