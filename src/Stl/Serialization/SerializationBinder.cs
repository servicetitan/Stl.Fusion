using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Stl.Concurrency;
using Stl.Reflection;

namespace Stl.Serialization
{
    public class SerializationBinder : ISerializationBinder
    {
        public static readonly ISerializationBinder Instance = new SerializationBinder();

        private readonly ConcurrentDictionary<(string? AssemblyName, string TypeName), Type> _cache;
        private readonly Func<(string?, string), Type> _resolveTypeHandler;

        public SerializationBinder()
        {
            _resolveTypeHandler = ResolveType;
            _cache = new ConcurrentDictionary<(string?, string), Type>();
        }

        public Type BindToType(string? assemblyName, string typeName)
            => GetType(assemblyName, typeName);

        public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
        {
            assemblyName = serializedType.GetTypeInfo().Assembly.FullName;
            typeName = serializedType.FullName;
        }

        // Protected part

        protected Type GetType(string? assemblyName, string typeName)
            => _cache.GetOrAddChecked((assemblyName, typeName), _resolveTypeHandler);

        protected virtual Type ResolveType((string? AssemblyName, string TypeName) key)
        {
            var (assemblyName, typeName) = key;

            if (assemblyName != null) {
                var assembly = Assembly.Load(assemblyName);
                if (assembly == null)
                    throw new JsonSerializationException(
                        $"Could not load assembly '{assemblyName}'.");

                var type = assembly.GetType(typeName);
                if (type == null) {
                    if (typeName.IndexOf('`') >= 0) {
                        try {
                            type = ResolveGenericType(typeName, assembly);
                        }
                        catch (Exception e) {
                            throw new JsonSerializationException(
                                $"Could not find type '{typeName}' in assembly '{assembly.FullName}'.", e);
                        }
                    }
                    if (type == null)
                        throw new JsonSerializationException(
                            $"Could not find type '{typeName}' in assembly '{assembly.FullName}'.");
                }
                return type;
            }
            return Type.GetType(typeName);
        }

        protected Type? ResolveGenericType(string typeName, Assembly assembly)
        {
            var openBracketIndex = typeName.IndexOf('[');
            string genericTypeDefName = typeName.Substring(0, openBracketIndex);
            if (openBracketIndex < 0)
                return null;

            var genericTypeDef = assembly.GetType(genericTypeDefName);
            if (genericTypeDef == null)
                return null;

            var genericTypeArguments = new List<Type>();
            var scope = 0;
            var typeArgStartIndex = 0;
            var endIndex = typeName.Length - 1;
            for (var i = openBracketIndex + 1; i < endIndex; ++i) {
                var current = typeName[i];
                switch (current) {
                case '[':
                    if (scope == 0)
                        typeArgStartIndex = i + 1;
                    ++scope;
                    break;
                case ']':
                    --scope;
                    if (scope == 0) {
                        string typeArgAssemblyQualifiedName = typeName.Substring(
                            typeArgStartIndex, i - typeArgStartIndex);
                        TypeNameHelpers.SplitAssemblyQualifiedName(typeArgAssemblyQualifiedName,
                            out var typeArgAssemblyName, out var typeArgTypeName);
                        genericTypeArguments.Add(GetType(typeArgAssemblyName, typeArgTypeName));
                    }
                    break;
                }
            }
            return genericTypeDef.MakeGenericType(genericTypeArguments.ToArray());
        }
    }
}
