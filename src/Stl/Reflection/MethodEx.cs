using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stl.Concurrency;

namespace Stl.Reflection
{
    public static class MethodEx
    {
        private static readonly ConcurrentDictionary<MethodInfo, MethodInfo?> BaseOrDeclaringMethodCache =
            new ConcurrentDictionary<MethodInfo, MethodInfo?>();

        private static MethodInfo GetDeclaringMethod(MethodInfo method)
        {
            if (method.IsConstructedGenericMethod)
                method = method.GetGenericMethodDefinition();
            if (method.ReflectedType != method.DeclaringType)
                method = method.GetBaseOrDeclaringMethod()!;
            return method;
        }

        public static MethodInfo? GetBaseOrDeclaringMethod(this MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (method.IsConstructedGenericMethod)
                return method.GetGenericMethodDefinition();
            if (!method.IsVirtual || method.IsStatic || method.DeclaringType.IsInterface)
                return null!;

            return BaseOrDeclaringMethodCache.GetOrAddChecked(method, method1 => {
                var declaringType = method.DeclaringType;
                var baseType = method.ReflectedType == declaringType
                    ? declaringType.BaseType
                    : declaringType;
                if (baseType == null)
                    return null;

                var veryBaseMethod = method1.GetBaseDefinition();
                var baseMethod = baseType.GetMethods().SingleOrDefault(m => m.GetBaseDefinition() == veryBaseMethod);
                return baseMethod;
            });
        }

        public static TAttr? GetAttribute<TAttr>(this MethodInfo method, bool inheritFromInterfaces, bool inheritFromBaseTypes)
            where TAttr : Attribute
        {
            if (method.IsConstructedGenericMethod)
                method = method.GetGenericMethodDefinition();
            return GetAttributeInternal<TAttr>(method, inheritFromInterfaces, inheritFromBaseTypes);
        }

        public static List<TAttr> GetAttributes<TAttr>(this MethodInfo method, bool inheritFromInterfaces, bool inheritFromBaseTypes)
            where TAttr : Attribute
        {
            if (method.IsConstructedGenericMethod)
                method = method.GetGenericMethodDefinition();
            var result = new List<TAttr>();
            AddAttributes(result, method, inheritFromInterfaces, inheritFromBaseTypes);
            return result;
        }

        private static TAttr? GetAttributeInternal<TAttr>(MethodInfo method, bool inheritFromInterfaces, bool inheritFromBaseTypes)
            where TAttr : Attribute
        {
            if (method.DeclaringType == method.ReflectedType) {
                var attr = method.GetCustomAttributes(false).OfType<TAttr>().FirstOrDefault();
                if (attr != null)
                    return attr;
            }
            var type = method.ReflectedType;
            var baseType = method.DeclaringType;
            if (baseType == type)
                baseType = type.BaseType;
            if (inheritFromInterfaces && !type.IsInterface) {
                var interfaces = type.GetInterfaces().AsEnumerable();
                if (baseType != null)
                    interfaces = interfaces.Except(baseType.GetInterfaces());
                foreach (var @interface in interfaces) {
                    var map = type.GetInterfaceMap(@interface);
                    for (var index = 0; index < map.TargetMethods.Length; index++) {
                        if (map.TargetMethods[index] != method)
                            continue;
                        var iMethod = map.InterfaceMethods[index];
                        var attr = iMethod.GetCustomAttributes(false).OfType<TAttr>().FirstOrDefault();
                        if (attr != null)
                            return attr;
                    }
                }
            }
            if (inheritFromBaseTypes && baseType != null) {
                var baseMethod = method.GetBaseOrDeclaringMethod();
                if (baseMethod == null)
                    return null;
                return GetAttributeInternal<TAttr>(baseMethod, inheritFromInterfaces, inheritFromBaseTypes);
            }
            return null;
        }

        private static void AddAttributes<TAttr>(List<TAttr> result, MethodInfo method, bool inheritFromInterfaces, bool inheritFromBaseTypes)
            where TAttr : Attribute
        {
            if (method.DeclaringType == method.ReflectedType)
                result.AddRange(method.GetCustomAttributes(false).OfType<TAttr>());
            var type = method.ReflectedType;
            var baseType = method.DeclaringType;
            if (baseType == type)
                baseType = type.BaseType;

            if (inheritFromInterfaces && !type.IsInterface) {
                var interfaces = type.GetInterfaces().AsEnumerable();
                if (baseType != null)
                    interfaces = interfaces.Except(baseType.GetInterfaces());
                foreach (var @interface in interfaces) {
                    var map = type.GetInterfaceMap(@interface);
                    for (var index = 0; index < map.TargetMethods.Length; index++) {
                        if (map.TargetMethods[index] != method)
                            continue;
                        var iMethod = map.InterfaceMethods[index];
                        result.AddRange(iMethod.GetCustomAttributes(false).OfType<TAttr>());
                    }
                }
            }
            if (inheritFromBaseTypes && baseType != null) {
                var baseMethod = method.GetBaseOrDeclaringMethod();
                if (baseMethod != null)
                    result.AddRange(baseMethod.GetAttributes<TAttr>(inheritFromInterfaces, inheritFromBaseTypes));
            }
        }
    }
}
