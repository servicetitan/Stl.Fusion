using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Reflection;

public static class MethodInfoExt
{
    private static readonly ConcurrentDictionary<MethodInfo, MethodInfo?> BaseOrDeclaringMethodCache = new();

    [RequiresUnreferencedCode(UnreferencedCode.Reflection)]
    public static MethodInfo? GetBaseOrDeclaringMethod(this MethodInfo method)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));
        if (method.IsConstructedGenericMethod())
            return method.GetGenericMethodDefinition();
        if (!method.IsVirtual || method.IsStatic || method.DeclaringType!.IsInterface)
            return null!;

        return BaseOrDeclaringMethodCache.GetOrAdd(method, method1 => {
            var declaringType = method1.DeclaringType;
            var baseType = method1.ReflectedType == declaringType
                ? declaringType!.BaseType
                : declaringType;
            if (baseType == null)
                return null;

            var veryBaseMethod = method1.GetBaseDefinition();
            var bindingFlags = BindingFlags.Instance
                | (method1.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic);
            var baseMethod = baseType
                .GetMethods(bindingFlags)
                .SingleOrDefault(m => m.GetBaseDefinition() == veryBaseMethod);
            return baseMethod;
        });
    }

    [RequiresUnreferencedCode(UnreferencedCode.Reflection)]
    public static TAttr? GetAttribute<TAttr>(this MethodInfo method, bool inheritFromInterfaces, bool inheritFromBaseTypes)
        where TAttr : Attribute
    {
        if (method.IsConstructedGenericMethod())
            method = method.GetGenericMethodDefinition();
        var methodDef = method.GetBaseDefinition();
        return GetAttributeInternal<TAttr>(method, methodDef, inheritFromInterfaces, inheritFromBaseTypes);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Reflection)]
    public static List<TAttr> GetAttributes<TAttr>(this MethodInfo method, bool inheritFromInterfaces, bool inheritFromBaseTypes)
        where TAttr : Attribute
    {
        if (method.IsConstructedGenericMethod())
            method = method.GetGenericMethodDefinition();
        var methodDef = method.GetBaseDefinition();
        var result = new List<TAttr>();
        AddAttributes(result, new HashSet<Type>(), method, methodDef, inheritFromInterfaces, inheritFromBaseTypes);
        return result;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Reflection)]
    private static TAttr? GetAttributeInternal<TAttr>(MethodInfo method, MethodInfo methodDef, bool inheritFromInterfaces, bool inheritFromBaseTypes)
        where TAttr : Attribute
    {
        var isEndOfChain = method == methodDef;
        if (isEndOfChain || method.DeclaringType == method.ReflectedType) {
            var attr = method.GetCustomAttributes(false).OfType<TAttr>().FirstOrDefault();
            if (attr != null)
                return attr;
        }
        var type = method.ReflectedType;
        var baseType = method.DeclaringType;
        if (baseType == type)
            baseType = type!.BaseType;

        if (inheritFromInterfaces && !type!.IsInterface) {
            var interfaces = type.GetInterfaces().ToHashSet();
            if (baseType != null)
                interfaces.ExceptWith(baseType.GetInterfaces());
            foreach (var @interface in interfaces) {
                var map = type.GetInterfaceMap(@interface);
                var targetMethods = map.TargetMethods;
                for (var index = 0; index < targetMethods.Length; index++) {
                    var targetMethod = targetMethods[index];
                    if (targetMethod != method)
                        continue;
                    var iMethod = map.InterfaceMethods[index];
                    var attr = iMethod.GetCustomAttributes(false).OfType<TAttr>().FirstOrDefault();
                    if (attr != null)
                        return attr;
                }
            }
        }
        if (inheritFromBaseTypes && baseType != null && !isEndOfChain) {
            var baseMethod = method.GetBaseOrDeclaringMethod();
            if (baseMethod == null)
                return null;
            return GetAttributeInternal<TAttr>(baseMethod, methodDef, inheritFromInterfaces, inheritFromBaseTypes);
        }
        return null;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Reflection)]
    private static void AddAttributes<TAttr>(List<TAttr> result, HashSet<Type> excluded, MethodInfo method, MethodInfo methodDef, bool inheritFromInterfaces, bool inheritFromBaseTypes)
        where TAttr : Attribute
    {
        var isEndOfChain = method == methodDef;
        if (isEndOfChain || method.DeclaringType == method.ReflectedType)
            result.AddRange(method.GetCustomAttributes(false).OfType<TAttr>());
        var type = method.ReflectedType;
        var baseType = method.DeclaringType;
        if (baseType == type)
            baseType = type!.BaseType;

        if (inheritFromInterfaces && !type!.IsInterface) {
            var interfaces = type.GetInterfaces().ToHashSet();
            if (baseType != null)
                interfaces.ExceptWith(baseType.GetInterfaces());
            foreach (var @interface in interfaces) {
                if (!excluded.Add(@interface))
                    continue;
                var map = type.GetInterfaceMap(@interface);
                var targetMethods = map.TargetMethods;
                for (var index = 0; index < targetMethods.Length; index++) {
                    var targetMethod = targetMethods[index];
                    if (targetMethod != method)
                        continue;
                    var iMethod = map.InterfaceMethods[index];
                    result.AddRange(iMethod.GetCustomAttributes(false).OfType<TAttr>());
                }
            }
        }
        if (inheritFromBaseTypes && baseType != null && !isEndOfChain) {
            var baseMethod = method.GetBaseOrDeclaringMethod();
            if (baseMethod != null)
                AddAttributes(result, excluded, baseMethod, methodDef, inheritFromInterfaces, inheritFromBaseTypes);
        }
    }
}
