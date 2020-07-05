using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Stl.Collections;
using Stl.Concurrency;
using Stl.Text;

namespace Stl.Reflection
{
    public static class TypeEx
    {
        public static readonly string SymbolPrefix = "@";

        private static readonly Regex MethodNameRe = new Regex("[^\\w\\d]+", RegexOptions.Compiled);
        private static readonly Regex MethodNameTailRe = new Regex("_+$", RegexOptions.Compiled);
        private static readonly Regex GenericMethodNameTailRe = new Regex("_\\d+$", RegexOptions.Compiled);
        private static readonly ConcurrentDictionary<(Type, bool, bool), Symbol> ToMethodNameCache =
            new ConcurrentDictionary<(Type, bool, bool), Symbol>();
        private static readonly ConcurrentDictionary<Type, Symbol> ToSymbolCache =
            new ConcurrentDictionary<Type, Symbol>();

        public static IEnumerable<Type> GetAllBaseTypes(this Type type, bool addSelf = false)
        {
            if (addSelf)
                yield return type;
            var baseType = type.BaseType;
            while (baseType != null) {
                yield return baseType;
                baseType = baseType.BaseType;
            }
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

        public static string ToMethodName(this Type type, bool useFullName = false, bool useFullArgumentNames = false)
        {
            var key = (type, useFullName, useFullArgumentNames);
            return ToMethodNameCache.GetOrAddChecked(key, key1 => {
                var (type1, useFullName1, useFullArgumentNames1) = key1;
                var name = useFullName1 ? type1.FullName : type1.Name;
                if (type1.IsGenericType && !type1.IsGenericTypeDefinition) {
                    name = type1.GetGenericTypeDefinition().ToMethodName(useFullName1);
                    name = GenericMethodNameTailRe.Replace(name, "");
                    var argumentNames = type1.GetGenericArguments()
                        .Select(t => t.ToMethodName(useFullArgumentNames1, useFullArgumentNames1));
                    name = string.Join('_', EnumerableEx.One(name).Concat(argumentNames));
                }
                name = MethodNameRe.Replace(name, "_");
                name = MethodNameTailRe.Replace(name, "");
                return name;
            });
        }

        public static Symbol ToSymbol(this Type type, bool withPrefix = true) 
            => withPrefix
                ? ToSymbolCache.GetOrAddChecked(type, type1 =>
                    new Symbol(SymbolPrefix + type1.ToMethodName(true, true)))
                : (Symbol) type.ToMethodName(true, true);
    }
}
