using System;
using Newtonsoft.Json.Serialization;
using Stl.Internal;
using Stl.Reflection;
using Stl.Serialization.Internal;
using Stl.Text;

namespace Stl.Serialization
{
    public class TypeDecoratingSerializer : Utf16SerializerBase
    {
        public static TypeDecoratingSerializer Default { get; } =
            new(SystemJsonSerializer.Default);

        private readonly ISerializationBinder _serializationBinder;

        public IUtf16Serializer Serializer { get; }
        public Func<Type, bool> TypeFilter { get; }

        public TypeDecoratingSerializer(IUtf16Serializer serializer, Func<Type, bool>? typeFilter = null)
        {
            Serializer = serializer;
            TypeFilter = typeFilter ?? (_ => true);
            var serializationBinder = (Serializer as NewtonsoftJsonSerializer)?.Settings?.SerializationBinder;
#if NET5_0
            serializationBinder ??= SerializationBinder.Instance;
#else
            serializationBinder ??= CrossPlatformSerializationBinder.Instance;
#endif
            _serializationBinder = serializationBinder;
        }

        public override object? Read(string data, Type type)
        {
            using var p = ListFormat.Default.CreateParser(data);

            p.ParseNext();
            if (string.IsNullOrEmpty(p.Item))
                // Special case: null deserialization
                return null;

            TypeNameHelpers.SplitAssemblyQualifiedName(p.Item, out var assemblyName, out var typeName);
            var actualType = _serializationBinder.BindToType(assemblyName, typeName);
            if (!TypeFilter.Invoke(actualType))
                throw Errors.UnsupportedTypeForJsonSerialization(actualType);
            if (!type.IsAssignableFrom(actualType))
                throw Errors.UnsupportedTypeForJsonSerialization(actualType);

            p.ParseNext();
            return Serializer.Reader.Read(p.Item, actualType);
        }

        public override string Write(object? value, Type type)
        {
            using var f = ListFormat.Default.CreateFormatter();
            if (value == null) {
                // Special case: null serialization
                f.Append("");
                f.AppendEnd();
            }
            else {
                var actualType = value.GetType();
                if (!type.IsAssignableFrom(actualType))
                    throw Errors.MustBeAssignableTo(actualType, type, nameof(type));
                if (!TypeFilter.Invoke(actualType))
                    throw Errors.UnsupportedTypeForJsonSerialization(actualType);
                var aqn = actualType.GetAssemblyQualifiedName(false, _serializationBinder);
                var json = Serializer.Writer.Write(value, actualType);
                f.Append(aqn);
                f.Append(json);
                f.AppendEnd();
            }
            return f.Output;
        }
    }
}
