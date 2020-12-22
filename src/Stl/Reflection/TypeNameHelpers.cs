using System;
using System.Text;
using Newtonsoft.Json.Serialization;
using Stl.Text;

namespace Stl.Reflection
{
    public static class TypeNameHelpers
    {
        public static string GetAssemblyQualifiedName(this Type type, bool fullName = true, ISerializationBinder? binder = null)
        {
            string aqn;
            if (binder == null)
                aqn = type.AssemblyQualifiedName!;
            else {
                binder.BindToName(type, out string? assemblyName, out string? typeName);
                aqn = typeName + (assemblyName == null ? "" : ", " + assemblyName);
            }
            return fullName ? aqn : RemoveAssemblyDetails(aqn);
        }

        public static void SplitAssemblyQualifiedName(string fullyQualifiedTypeName, out string? assemblyName, out string typeName)
        {
            var assemblyDelimiterIndex = GetAssemblyDelimiterIndex(fullyQualifiedTypeName);
            if (assemblyDelimiterIndex.HasValue) {
                typeName = fullyQualifiedTypeName.AsSpan()
                    .Slice(0, assemblyDelimiterIndex.GetValueOrDefault())
                    .Trim()
                    .ToString();
                assemblyName = fullyQualifiedTypeName.AsSpan()
                    .Slice(
                        assemblyDelimiterIndex.GetValueOrDefault() + 1,
                        fullyQualifiedTypeName.Length - assemblyDelimiterIndex.GetValueOrDefault() - 1)
                    .Trim()
                    .ToString();
            }
            else {
                typeName = fullyQualifiedTypeName;
                assemblyName = null;
            }
        }

        private static string RemoveAssemblyDetails(string fullyQualifiedTypeName)
        {
            var sb = StringBuilderEx.Acquire(0x20);
            var writingAssemblyName = false;
            var skipping = false;
            foreach (var c in fullyQualifiedTypeName) {
                switch (c) {
                case '[':
                case ']':
                    writingAssemblyName = false;
                    skipping = false;
                    sb.Append(c);
                    break;
                case ',':
                    if (!writingAssemblyName) {
                        writingAssemblyName = true;
                        sb.Append(c);
                    }
                    else
                        skipping = true;
                    break;
                default:
                    if (!skipping)
                        sb.Append(c);
                    break;
                }
            }
            return sb.ToStringAndRelease();
        }

        private static int? GetAssemblyDelimiterIndex(string fullyQualifiedTypeName)
        {
            var scope = 0;
            for (var i = 0; i < fullyQualifiedTypeName.Length; i++) {
                var current = fullyQualifiedTypeName[i];
                switch (current) {
                case '[':
                    scope++;
                    break;
                case ']':
                    scope--;
                    break;
                case ',':
                    if (scope == 0)
                        return i;
                    break;
                }
            }
            return null;
        }
    }
}
