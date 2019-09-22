using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Stl.Reflection
{
    public static class TypeEx
    {
        private static readonly Regex MethodNameRe = new Regex("[^\\w\\d]+", RegexOptions.Compiled);
        private static readonly Regex MethodNameTailRe = new Regex("_+$", RegexOptions.Compiled);
        private static readonly Regex GenericMethodNameTailRe = new Regex("_\\d+$", RegexOptions.Compiled);

        public static IEnumerable<Type> GetAllBaseTypes(this Type type)
        {
            var baseType = type.BaseType;
            while (baseType != null) {
                yield return baseType;
                baseType = baseType.BaseType;
            }
        }

        public static string ToMethodName(this Type type, bool useFullName = false, bool useFullArgumentNames = false)
        {
            var name = useFullName ? type.FullName : type.Name;
            if (type.IsGenericType && !type.IsGenericTypeDefinition) {
                name = type.GetGenericTypeDefinition().ToMethodName(useFullName);
                name = GenericMethodNameTailRe.Replace(name, "");
                var argumentNames = type.GetGenericArguments()
                    .Select(t => t.ToMethodName(useFullArgumentNames, useFullArgumentNames));
                name = string.Join('_', EnumerableEx.One(name).Concat(argumentNames));
            }

            name = MethodNameRe.Replace(name, "_");
            name = MethodNameTailRe.Replace(name, "");
            return name;
        }
    }
}
