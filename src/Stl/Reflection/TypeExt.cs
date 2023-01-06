using System.Text.RegularExpressions;
using Cysharp.Text;
using Stl.Concurrency;

namespace Stl.Reflection;

public static class TypeExt
{
    private static readonly Regex MethodNameRe = new("[^\\w\\d]+", RegexOptions.Compiled);
    private static readonly Regex MethodNameTailRe = new("_+$", RegexOptions.Compiled);
    private static readonly Regex GenericTypeNameTailRe = new("`.+$", RegexOptions.Compiled);
    private static readonly ConcurrentDictionary<(Type, bool, bool), Symbol> GetNameCache = new();
    private static readonly ConcurrentDictionary<(Type, bool, bool), Symbol> ToIdentifierNameCache = new();
    private static readonly ConcurrentDictionary<Type, Symbol> ToSymbolCache = new();
    private static readonly ConcurrentDictionary<Type, Type?> GetTaskOrValueTaskTypeCache = new();

    public static readonly string SymbolPrefix = "@";
    public static Func<Type, bool> ProxyTypeDetector { get; set; } =
        type => "Castle.Proxies".Equals(type.Namespace, StringComparison.Ordinal);

    public static Type NonProxyType(this Type type)
        => ProxyTypeDetector(type) ? NonProxyType(type.BaseType!) : type;

    public static IEnumerable<Type> GetAllBaseTypes(this Type type, bool addSelf = false, bool addInterfaces = false)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (addSelf)
            yield return type;
        var baseType = type.BaseType;
        if (baseType == null)
            yield break; // type == typeof(Object)

        while (baseType != typeof(object)) {
            yield return baseType!;
            baseType = baseType!.BaseType;
        }
        if (addInterfaces) {
            var interfaces = type.GetInterfaces();
            var orderedInterfaces = interfaces
                .OrderBy(i => -i.GetInterfaces().Length)
                .OrderByDependency(i => interfaces.Where(j => i != j && j.IsAssignableFrom(i)))
                .Reverse();
            foreach (var @interface in orderedInterfaces)
                yield return @interface;
        }
        yield return typeof(object);
    }

    public static bool MayCastSucceed(this Type castFrom, Type castTo)
    {
        if (castTo.IsSealed || castTo.IsValueType)
            // AnyBase(SealedType) -> SealedType
            return castFrom.IsAssignableFrom(castTo);
        if (castFrom.IsSealed || castFrom.IsValueType)
            // SealedType -> AnyBase(SealedType)
            return castTo.IsAssignableFrom(castFrom);
        if (castTo.IsInterface || castFrom.IsInterface)
            // Not super obvious, but true
            return true;

        // Both types are classes, so the cast may succeed
        // only if one of them is a base of another
        return castTo.IsAssignableFrom(castFrom) || castFrom.IsAssignableFrom(castTo);
    }

    public static string GetName(this Type type, bool useFullName = false, bool useFullArgumentNames = false)
    {
        var key = (type, useFullName, useFullArgumentNames);
        return GetNameCache.GetOrAddChecked(key,
            static key1 => {
                var (type1, useFullName1, useFullArgumentNames1) = key1;
                var name = type1.Name;
                if (type1.IsGenericTypeDefinition) {
                    name = GenericTypeNameTailRe.Replace(name, "");
                    var argumentNames = type1.GetGenericArguments().Select(t => t.Name);
                    name = $"{name}<{argumentNames.ToDelimitedString(",")}>";
                }
                else if (type1.IsGenericType) {
                    name = GenericTypeNameTailRe.Replace(name, "");
                    var argumentNames = type1.GetGenericArguments()
                        .Select(t => t.GetName(useFullArgumentNames1, useFullArgumentNames1));
                    name = $"{name}<{argumentNames.ToDelimitedString(",")}>";
                }
                if (type1.DeclaringType != null)
                    name = $"{type1.DeclaringType.GetName(useFullName1)}+{name}";
                else if (useFullName1)
                    name = $"{type1.Namespace}.{name}";
                return name;
            });
    }

    public static string ToIdentifierName(this Type type, bool useFullName = false, bool useFullArgumentNames = false)
    {
        var key = (type, useFullName, useFullArgumentNames);
        return ToIdentifierNameCache.GetOrAddChecked(key,
            static key1 => {
                var (type1, useFullName1, useFullArgumentNames1) = key1;
                var name = type1.Name;
                if (type1.IsGenericTypeDefinition)
                    name = $"{GenericTypeNameTailRe.Replace(name, "")}_{type1.GetGenericArguments().Length}";
                else if (type1.IsGenericType) {
                    name = GenericTypeNameTailRe.Replace(name, "");
                    var argumentNames = type1.GetGenericArguments()
                        .Select(t => t.ToIdentifierName(useFullArgumentNames1, useFullArgumentNames1));
                    name = string.Join("_", EnumerableExt.One(name).Concat(argumentNames));
                }
                if (type1.DeclaringType != null)
                    name = $"{type1.DeclaringType.ToIdentifierName(useFullName1)}_{name}";
                else if (useFullName1)
                    name = $"{type1.Namespace}_{name}";
                name = MethodNameRe.Replace(name, "_");
                name = MethodNameTailRe.Replace(name, "");
                return name;
            });
    }

    public static Symbol ToSymbol(this Type type, bool withPrefix = true)
        => withPrefix
            ? ToSymbolCache.GetOrAddChecked(type,
                static type1 => new Symbol(SymbolPrefix + type1.ToIdentifierName(true, true)))
            : (Symbol) type.ToIdentifierName(true, true);

    public static bool IsTaskOrValueTask(this Type type)
        => type.GetTaskOrValueTaskType() != null;

    public static Type? GetTaskOrValueTaskType(this Type type)
    {
        return GetTaskOrValueTaskTypeCache.GetOrAdd(type, type1 => {
            if (type1 == typeof(object))
                return null;
            if (type1 == typeof(ValueTask) || type1 == typeof(Task))
                return type1;
            if (type1.IsGenericType) {
                var gtd = type1.GetGenericTypeDefinition();
                if (gtd == typeof(ValueTask<>) || gtd == typeof(Task<>))
                    return type1;
            }

            var baseType = type1.BaseType;
            return baseType == null ? null : GetTaskOrValueTaskType(baseType);
        });
    }

    public static Type? GetTaskOrValueTaskArgument(this Type type)
    {
        var taskType = type.GetTaskOrValueTaskType();
        if (taskType == null)
            throw new ArgumentOutOfRangeException(nameof(type));
        return taskType.IsGenericType
            ? taskType.GenericTypeArguments.SingleOrDefault()
            : null;
    }
}
