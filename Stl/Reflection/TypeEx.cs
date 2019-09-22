using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Stl.Reflection
{
    public static class TypeEx
    {
        private static readonly Regex MethodNameRe = new Regex("[^\\w\\d]+", RegexOptions.Compiled);
        private static readonly Regex MethodNameEndRe = new Regex("_+$", RegexOptions.Compiled);

        public static IEnumerable<Type> GetAllBaseTypes(this Type type)
        {
            var baseType = type.BaseType;
            while (baseType != null) {
                yield return baseType;
                baseType = baseType.BaseType;
            }
        }

        public static string ToMethodName(this Type type, bool useFullName = false)
        {
            var name = useFullName ? type.FullName : type.Name;
            name = MethodNameRe.Replace(name, "_");
            name = MethodNameEndRe.Replace(name, "");
            return name;
        }
    }
}
