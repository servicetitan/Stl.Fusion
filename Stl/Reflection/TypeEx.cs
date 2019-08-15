using System;
using System.Collections.Generic;

namespace Stl.Reflection
{
    public static class TypeEx
    {
        public static IEnumerable<Type> GetAllBaseTypes(this Type type)
        {
            var baseType = type.BaseType;
            while (baseType != null) {
                yield return baseType;
                baseType = baseType.BaseType;
            }
        }
    }
}
