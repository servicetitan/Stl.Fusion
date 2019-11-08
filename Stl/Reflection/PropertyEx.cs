using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Stl.Reflection
{
    public static class PropertyEx
    {
        private static readonly ConcurrentDictionary<(Type, Symbol, bool), Delegate> _getterCache =
            new ConcurrentDictionary<(Type, Symbol, bool), Delegate>();
        private static readonly ConcurrentDictionary<(Type, Symbol, bool), Delegate> _setterCache =
            new ConcurrentDictionary<(Type, Symbol, bool), Delegate>();

        public static Func<object, TProperty> GetGetter<TProperty>(this Type type, Symbol propertyName, bool isValueUntyped = false)
            => (Func<object, TProperty>) GetGetter(type, propertyName, isValueUntyped);
        public static Delegate GetGetter(this Type type, Symbol propertyName, bool isValueUntyped = false)
        {
            var key = (type, propertyName, isValueUntyped);
            if (_getterCache.TryGetValue(key, out var r))
                return r;
            lock (_getterCache) {
                if (_getterCache.TryGetValue(key, out r))
                    return r;
                var pObject = Expression.Parameter(typeof(object), "object");
                var eAccess = Expression.PropertyOrField(
                    Expression.ConvertChecked(pObject, type), 
                    propertyName.Value);
                var body = isValueUntyped
                    ? (Expression) Expression.Convert(eAccess, typeof(object))
                    : eAccess;
                r = Expression.Lambda(body, pObject).Compile();
                _getterCache[key] = r;
                return r;
            }
        }
        
        public static Action<object, TProperty> GetSetter<TProperty>(this Type type, Symbol propertyName, bool isValueUntyped = false)
            => (Action<object, TProperty>) GetSetter(type, propertyName, isValueUntyped);
        public static Delegate GetSetter(this Type type, Symbol propertyName, bool isValueUntyped = false)
        {
            var key = (type, propertyName, isValueUntyped);
            if (_setterCache.TryGetValue(key, out var r))
                return r;
            lock (_setterCache) {
                if (_setterCache.TryGetValue(key, out r))
                    return r;
                var pObject = Expression.Parameter(typeof(object), "object");
                var lValue = Expression.PropertyOrField(
                    Expression.ConvertChecked(pObject, type), 
                    propertyName.Value);
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
    }
}
