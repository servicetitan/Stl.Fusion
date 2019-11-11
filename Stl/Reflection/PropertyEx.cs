using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Stl.Reflection
{
    public static class PropertyEx
    {
        private static readonly ConcurrentDictionary<(Type, Symbol, bool), Delegate?> _getterBySymCache =
            new ConcurrentDictionary<(Type, Symbol, bool), Delegate?>();
        private static readonly ConcurrentDictionary<(Type, Symbol, bool), Delegate?> _setterBySymCache =
            new ConcurrentDictionary<(Type, Symbol, bool), Delegate?>();
        private static readonly ConcurrentDictionary<(Type, PropertyInfo, bool), Delegate> _getterCache =
            new ConcurrentDictionary<(Type, PropertyInfo, bool), Delegate>();
        private static readonly ConcurrentDictionary<(Type, PropertyInfo, bool), Delegate> _setterCache =
            new ConcurrentDictionary<(Type, PropertyInfo, bool), Delegate>();
        private static readonly ConcurrentDictionary<(Type, Delegate, BindingFlags), ReadOnlyMemory<Symbol>> _findPropertiesCache =
            new ConcurrentDictionary<(Type, Delegate, BindingFlags), ReadOnlyMemory<Symbol>>();

        // Note that predicate is used as cache key here, so you shouldn't pass
        // closure predicates into this method!  
        public static ReadOnlyMemory<Symbol> FindProperties(this Type type, Func<PropertyInfo, bool> predicate, 
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
        {
            var key = (type, predicate, bindingFlags);
            if (_findPropertiesCache.TryGetValue(key, out var r))
                return r;
            lock (_findPropertiesCache) {
                if (_findPropertiesCache.TryGetValue(key, out r))
                    return r;
                r = (
                    from property in type.GetProperties(bindingFlags) 
                    where predicate.Invoke(property) 
                    select new Symbol(property.Name)
                    ).ToArray();
                _findPropertiesCache[key] = r;
                return r;
            }
        }

        public static T Get<T>(object target, Symbol propertyName) 
            => GetGetter<T>(target.GetType(), propertyName)!.Invoke(target);
        public static object GetUntyped(object target, Symbol propertyName) 
            => GetGetter<object>(target.GetType(), propertyName, true)!.Invoke(target);

        public static void Set<T>(object target, Symbol propertyName, T value) 
            => GetSetter<T>(target.GetType(), propertyName)!.Invoke(target, value);
        public static void SetUntyped(object target, Symbol propertyName, object value) 
            => GetSetter<object>(target.GetType(), propertyName, true)!.Invoke(target, value);

        public static Func<object, TProperty>? GetGetter<TProperty>(this Type type, Symbol propertyName, bool isValueUntyped = false)
            => (Func<object, TProperty>?) GetGetter(type, propertyName, isValueUntyped);
        
        public static Delegate? GetGetter(this Type type, Symbol propertyName, bool isValueUntyped = false)
        {
            var key = (type, propertyName, isValueUntyped);
            if (_getterBySymCache.TryGetValue(key, out var r))
                return r;
            lock (_getterBySymCache) {
                if (_getterBySymCache.TryGetValue(key, out r))
                    return r;
                var pi = GetProperty(type, propertyName);
                r = pi == null ? null : type.GetGetter(pi, isValueUntyped);
                _getterBySymCache[key] = r;
                return r;
            }
        }

        public static Delegate GetGetter(this Type type, PropertyInfo propertyInfo, bool isValueUntyped = false)
        {
            var key = (type, propertyInfo, isValueUntyped);
            if (_getterCache.TryGetValue(key, out var r))
                return r;
            lock (_getterCache) {
                if (_getterCache.TryGetValue(key, out r))
                    return r;
                var pObject = Expression.Parameter(typeof(object), "object");
                var eAccess = Expression.Property(
                    Expression.ConvertChecked(pObject, type), 
                    propertyInfo);
                var body = isValueUntyped
                    ? (Expression) Expression.Convert(eAccess, typeof(object))
                    : eAccess;
                r = Expression.Lambda(body, pObject).Compile();
                _getterCache[key] = r;
                return r;
            }
        }
        
        public static Action<object, TProperty>? GetSetter<TProperty>(this Type type, Symbol propertyName, bool isValueUntyped = false)
            => (Action<object, TProperty>?) GetSetter(type, propertyName, isValueUntyped);

        public static Delegate? GetSetter(this Type type, Symbol propertyName, bool isValueUntyped = false)
        {
            var key = (type, propertyName, isValueUntyped);
            if (_setterBySymCache.TryGetValue(key, out var r))
                return r;
            lock (_setterBySymCache) {
                if (_setterBySymCache.TryGetValue(key, out r))
                    return r;
                var pi = GetProperty(type, propertyName);
                r = pi == null ? null : type.GetSetter(pi, isValueUntyped);
                _setterBySymCache[key] = r;
                return r;
            }
        }

        public static Delegate GetSetter(this Type type, PropertyInfo propertyInfo, bool isValueUntyped = false)
        {
            var key = (type, propertyInfo, isValueUntyped);
            if (_setterCache.TryGetValue(key, out var r))
                return r;
            lock (_setterCache) {
                if (_setterCache.TryGetValue(key, out r))
                    return r;
                var pObject = Expression.Parameter(typeof(object), "object");
                var lValue = Expression.Property(
                    Expression.ConvertChecked(pObject, type), 
                    propertyInfo);
                var pValue = Expression.Parameter(lValue.Type, "value");
                var rValue = (Expression) pValue;
                if (isValueUntyped) {
                    pValue = Expression.Parameter(typeof(object), "value");
                    rValue = Expression.ConvertChecked(pValue, lValue.Type);
                }
                var eAssign = Expression.Assign(lValue, rValue);
                var eReturnTarget = Expression.Label();
                var eBlock = Expression.Block(
                    eAssign, 
                    Expression.Return(eReturnTarget),
                    Expression.Label(eReturnTarget));
                r = Expression.Lambda(eBlock, pObject, pValue).Compile();
                _setterCache[key] = r;
                return r;
            }
        }

        // Prefers public properties over private ones
        public static PropertyInfo? GetProperty(Type type, Symbol propertyName) 
            => type.GetProperty(propertyName, 
                   BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy)
               ?? type.GetProperty(propertyName, 
                   BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);
    }
}
