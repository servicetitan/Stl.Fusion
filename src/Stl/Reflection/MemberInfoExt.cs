using System.Linq.Expressions;
using Stl.Reflection.Internal;

namespace Stl.Reflection;

public static class MemberInfoExt
{
    private static readonly ConcurrentDictionary<(MemberInfo, Type, bool), Delegate> GetterCache = new();
    private static readonly ConcurrentDictionary<(MemberInfo, Type, bool), Delegate> SetterCache = new();

    public static Func<TType, TValue> GetGetter<TType, TValue>(
        this MemberInfo propertyOrFieldInfo, bool isValueUntyped = false)
        => (Func<TType, TValue>) GetGetter(propertyOrFieldInfo, typeof(TType), isValueUntyped);
    public static Func<object, object> GetGetter(this MemberInfo propertyOrFieldInfo)
        => (Func<object, object>) GetGetter(propertyOrFieldInfo, typeof(object), true);

    public static Action<TType, TValue> GetSetter<TType, TValue>(
        this MemberInfo propertyOrFieldInfo, bool isValueUntyped = false)
        => (Action<TType, TValue>) GetSetter(propertyOrFieldInfo, typeof(TType), isValueUntyped);
    public static Action<object, object> GetSetter(
        this MemberInfo propertyOrFieldInfo, bool isValueUntyped = false)
        => (Action<object, object>) GetSetter(propertyOrFieldInfo, typeof(object), true);

    public static Delegate GetGetter(this MemberInfo propertyOrFieldInfo, Type sourceType, bool isValueUntyped = false)
    {
        var key = (propertyOrFieldInfo, sourceType, isValueUntyped);
        return GetterCache.GetOrAdd(key, static key1 => {
            var (propertyOrFieldInfo1, sourceType1, isValueUntyped1) = key1;
            var type = propertyOrFieldInfo1.DeclaringType!;
            var fi = propertyOrFieldInfo1 as FieldInfo;
            var pi = propertyOrFieldInfo1 as PropertyInfo;
            if (fi == null && pi == null)
                throw Errors.PropertyOrFieldInfoExpected(nameof(propertyOrFieldInfo));

            var pSource = Expression.Parameter(sourceType1, "source");
            var eSource = (Expression) (type.IsAssignableFrom(sourceType1) ? pSource : Expression.Convert(pSource, type));
            var rValue = fi != null
                ? (Expression) Expression.Field(eSource, fi)
                : Expression.Property(eSource, pi!);

            var body = isValueUntyped1
                ? Expression.Convert(rValue, typeof(object))
                : rValue;
            return Expression.Lambda(body, pSource).Compile();
        });
    }

    public static Delegate GetSetter(this MemberInfo propertyOrFieldInfo, Type sourceType, bool isValueUntyped = false)
    {
        var key = (propertyOrFieldInfo, sourceType, isValueUntyped);
        // ReSharper disable once InconsistentlySynchronizedField
        return SetterCache.GetOrAdd(key, static key1 => {
            var (propertyOrFieldInfo1, sourceType1, isValueUntyped1) = key1;
            var type = propertyOrFieldInfo1.DeclaringType!;
            var fi = propertyOrFieldInfo1 as FieldInfo;
            var pi = propertyOrFieldInfo1 as PropertyInfo;
            if (fi == null && pi == null)
                throw Errors.PropertyOrFieldInfoExpected(nameof(propertyOrFieldInfo));

            var pSource = Expression.Parameter(sourceType1, "source");
            var eSource = (Expression) (type.IsAssignableFrom(sourceType1) ? pSource : Expression.Convert(pSource, type));
            var lValue = fi != null
                ? (Expression) Expression.Field(eSource, fi)
                : Expression.Property(eSource, pi!);

            var pValue = Expression.Parameter(lValue.Type, "value");
            var rValue = (Expression) pValue;
            if (isValueUntyped1) {
                pValue = Expression.Parameter(typeof(object), "value");
                rValue = Expression.Convert(pValue, lValue.Type);
            }

            var eAssign = Expression.Assign(lValue, rValue);
            var eReturnTarget = Expression.Label();
            var eBlock = Expression.Block(
                eAssign,
                Expression.Return(eReturnTarget),
                Expression.Label(eReturnTarget));
            return Expression.Lambda(eBlock, pSource, pValue).Compile();
        });
    }
}
