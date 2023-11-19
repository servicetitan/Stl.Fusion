using System.Reflection.Emit;
using Stl.Internal;

namespace Stl.Reflection;

public static class MemberInfoExt
{
    private static readonly ConcurrentDictionary<(MemberInfo, Type, bool), Delegate> GetterCache = new();
    private static readonly ConcurrentDictionary<(MemberInfo, Type, bool), Delegate> SetterCache = new();

    public static Func<TType, TValue> GetGetter<TType, TValue>(
        this MemberInfo propertyOrField, bool isValueUntyped = false)
        => (Func<TType, TValue>) GetGetter(propertyOrField, typeof(TType), isValueUntyped);
    public static Func<object, TValue> GetGetter<TValue>(
        this MemberInfo propertyOrField, bool isValueUntyped = false)
        => (Func<object, TValue>) GetGetter(propertyOrField, typeof(object), isValueUntyped);
    public static Func<object, object> GetGetter(this MemberInfo propertyOrField)
        => (Func<object, object>) GetGetter(propertyOrField, typeof(object), true);

    public static Action<TType, TValue> GetSetter<TType, TValue>(
        this MemberInfo propertyOrField, bool isValueUntyped = false)
        => (Action<TType, TValue>) GetSetter(propertyOrField, typeof(TType), isValueUntyped);
    public static Action<object, TValue> GetSetter<TValue>(
        this MemberInfo propertyOrField, bool isValueUntyped = false)
        => (Action<object, TValue>) GetSetter(propertyOrField, typeof(object), isValueUntyped);
    public static Action<object, object> GetSetter(this MemberInfo propertyOrField)
        => (Action<object, object>) GetSetter(propertyOrField, typeof(object), true);

    public static Delegate GetGetter(this MemberInfo propertyOrField, Type sourceType, bool isValueUntyped = false)
    {
        var key = (propertyOrField, sourceType, isValueUntyped);
        return GetterCache.GetOrAdd(key, static key1 => {
            var (propertyOrField1, sourceType1, isValueUntyped1) = key1;
            var valueType = isValueUntyped1 ? typeof(object) : propertyOrField1.ReturnType();
            return CreateGetter(sourceType1, propertyOrField1, valueType);
        });
    }

    private static Delegate GetSetter(this MemberInfo propertyOrField, Type sourceType, bool isValueUntyped = false)
    {
        var key = (propertyOrField, sourceType, isValueUntyped);
        return SetterCache.GetOrAdd(key, static key1 => {
            var (propertyOrField1, sourceType1, isValueUntyped1) = key1;
            var valueType = isValueUntyped1 ? typeof(object) : propertyOrField1.ReturnType();
            return CreateSetter(sourceType1, propertyOrField1, valueType);
        });
    }

    private static Delegate CreateGetter(
        Type sourceType,
        MemberInfo propertyOrField,
        Type valueType)
    {
        var funcType = typeof(Func<,>).MakeGenericType(sourceType, valueType);
        var declaringType = propertyOrField.DeclaringType!;
        DynamicMethod m;
        ILGenerator il;
        if (propertyOrField is PropertyInfo pi) {
            var isStatic = pi.GetMethod!.IsStatic;
            if (!isStatic && declaringType.IsAssignableFrom(sourceType) && valueType == pi.PropertyType)
                return pi.GetMethod!.CreateDelegate(funcType);

            m = new DynamicMethod("_Getter", valueType, new[] { sourceType }, true);
            il = m.GetILGenerator();
            if (isStatic)
                il.Emit(OpCodes.Call, pi.GetMethod!);
            else {
                il.Emit(OpCodes.Ldarg_0);
                EmitCast(il, sourceType, declaringType);
                il.Emit(OpCodes.Callvirt, pi.GetMethod!);
            }
        }
        else if (propertyOrField is FieldInfo fi) {
            m = new DynamicMethod("_Getter", valueType, new[] { sourceType }, true);
            il = m.GetILGenerator();
            if (fi.IsStatic)
                il.Emit(OpCodes.Ldsfld, fi);
            else {
                il.Emit(OpCodes.Ldarg_0);
                EmitCast(il, sourceType, declaringType);
                il.Emit(OpCodes.Ldfld, fi);
            }
        }
        else
            throw new ArgumentOutOfRangeException(nameof(propertyOrField));

        EmitCast(il, propertyOrField.ReturnType(), valueType);
        il.Emit(OpCodes.Ret);
        return m.CreateDelegate(funcType);
    }

    private static Delegate CreateSetter(
        Type sourceType,
        MemberInfo propertyOrField,
        Type valueType)
    {
        var funcType = typeof(Action<,>).MakeGenericType(sourceType, valueType);
        var declaringType = propertyOrField.DeclaringType!;
        DynamicMethod m;
        ILGenerator il;
        if (propertyOrField is PropertyInfo pi) {
            var isStatic = pi.SetMethod!.IsStatic;
            if (!isStatic && declaringType.IsAssignableFrom(sourceType) && valueType == pi.PropertyType)
                return pi.SetMethod!.CreateDelegate(funcType);

            m = new DynamicMethod("_Setter", null, new[] { sourceType, valueType }, true);
            il = m.GetILGenerator();
            if (isStatic) {
                il.Emit(OpCodes.Ldarg_1);
                EmitCast(il, valueType, propertyOrField.ReturnType());
                il.Emit(OpCodes.Call, pi.SetMethod!);
            }
            else {
                il.Emit(OpCodes.Ldarg_0);
                EmitCast(il, sourceType, declaringType);
                il.Emit(OpCodes.Ldarg_1);
                EmitCast(il, valueType, propertyOrField.ReturnType());
                il.Emit(OpCodes.Callvirt, pi.SetMethod!);
            }
        }
        else if (propertyOrField is FieldInfo fi) {
            m = new DynamicMethod("_Setter", null, new[] { sourceType, valueType }, true);
            il = m.GetILGenerator();
            if (fi.IsStatic) {
                il.Emit(OpCodes.Ldarg_1);
                EmitCast(il, valueType, propertyOrField.ReturnType());
                il.Emit(OpCodes.Stsfld, fi);
            }
            else {
                il.Emit(OpCodes.Ldarg_0);
                EmitCast(il, sourceType, declaringType);
                il.Emit(OpCodes.Ldarg_1);
                EmitCast(il, valueType, propertyOrField.ReturnType());
                il.Emit(OpCodes.Stfld, fi);
            }
        }
        else
            throw new ArgumentOutOfRangeException(nameof(propertyOrField));

        il.Emit(OpCodes.Ret);
        return m.CreateDelegate(funcType);
    }

    private static void EmitCast(ILGenerator il, Type fromType, Type toType)
    {
        if (fromType == toType)
            return;

        if (toType.IsAssignableFrom(fromType)) {
            // Upcast (... -> base)
            if (!fromType.IsValueType)
                return; // No cast is needed in this case

            // struct -> Object
            il.Emit(OpCodes.Box, fromType);
            return;
        }

        if (!fromType.IsAssignableFrom(toType)) // Cast between two types which aren't related
            throw Errors.MustBeAssignableTo(fromType, toType, nameof(toType));

        // Downcast (base -> ...)
        il.Emit(OpCodes.Castclass, toType);
        if (toType.IsValueType)
            il.Emit(OpCodes.Unbox_Any, toType);
    }
}
