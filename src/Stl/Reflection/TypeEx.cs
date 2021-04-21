using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Stl.Collections;
using Stl.Concurrency;
using Stl.Text;

namespace Stl.Reflection
{
    public static class TypeEx
    {
        public static readonly string SymbolPrefix = "@";

        private static readonly Regex MethodNameRe = new("[^\\w\\d]+", RegexOptions.Compiled);
        private static readonly Regex MethodNameTailRe = new("_+$", RegexOptions.Compiled);
        private static readonly Regex GenericMethodNameTailRe = new("_\\d+$", RegexOptions.Compiled);
        private static readonly ConcurrentDictionary<(Type, bool, bool), Symbol> ToIdentifierNameCache = new();
        private static readonly ConcurrentDictionary<Type, Symbol> ToSymbolCache = new();

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

        public static string ToIdentifierName(this Type type, bool useFullName = false, bool useFullArgumentNames = false)
        {
            var key = (type, useFullName, useFullArgumentNames);
            return ToIdentifierNameCache.GetOrAddChecked(key, key1 => {
                var (type1, useFullName1, useFullArgumentNames1) = key1;
                var name = useFullName1 ? type1.FullName : type1.Name;
                if (type1.IsGenericType && !type1.IsGenericTypeDefinition) {
                    name = type1.GetGenericTypeDefinition().ToIdentifierName(useFullName1);
                    name = GenericMethodNameTailRe.Replace(name, "");
                    var argumentNames = type1.GetGenericArguments()
                        .Select(t => t.ToIdentifierName(useFullArgumentNames1, useFullArgumentNames1));
                    name = string.Join('_', EnumerableEx.One(name).Concat(argumentNames));
                }
                name = MethodNameRe.Replace(name!, "_");
                name = MethodNameTailRe.Replace(name, "");
                return name;
            });
        }

        public static Symbol ToSymbol(this Type type, bool withPrefix = true)
            => withPrefix
                ? ToSymbolCache.GetOrAddChecked(type, type1 =>
                    new Symbol(SymbolPrefix + type1.ToIdentifierName(true, true)))
                : (Symbol) type.ToIdentifierName(true, true);

        public static bool IsTaskOrValueTask(this Type type)
            => typeof(Task).IsAssignableFrom(type) || (
                type.IsGenericType
                    ? type.GetGenericTypeDefinition() == typeof(ValueTask<>)
                    : type == typeof(ValueTask));

        public static Type? GetTaskOrValueTaskArgument(this Type type)
        {
            if (!type.IsTaskOrValueTask())
                throw new ArgumentOutOfRangeException(nameof(type));
            return type.IsGenericType
                ? type.GenericTypeArguments.SingleOrDefault()
                : null;
        }
    }
}
