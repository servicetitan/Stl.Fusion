using System.Linq.Expressions;
using Stl.Internal;

namespace Stl.Reflection;

public static class ActivatorExt
{
    private static readonly ConcurrentDictionary<Type, bool> HasDefaultCtorCache = new();
    private static readonly ConcurrentDictionary<Type, Delegate?> CtorDelegate0Cache = new();
    private static readonly ConcurrentDictionary<(Type, Type), Delegate?> CtorDelegate1Cache = new();
    private static readonly ConcurrentDictionary<(Type, Type, Type), Delegate?> CtorDelegate2Cache = new();
    private static readonly ConcurrentDictionary<(Type, Type, Type, Type), Delegate?> CtorDelegate3Cache = new();
    private static readonly ConcurrentDictionary<(Type, Type, Type, Type, Type), Delegate?> CtorDelegate4Cache = new();
    private static readonly ConcurrentDictionary<(Type, Type, Type, Type, Type, Type), Delegate?> CtorDelegate5Cache = new();

    // An alternative to "new()" constraint
    public static T New<T>(bool failIfNoDefaultConstructor = true)
    {
        var type = typeof(T);
        if (type.IsValueType)
            return default!;
        var hasDefaultCtor = HasDefaultCtorCache.GetOrAdd(type,
            type1 => type1.GetConstructor(Array.Empty<Type>()) != null);
        if (hasDefaultCtor)
            return (T) type.CreateInstance();
        if (failIfNoDefaultConstructor)
            throw Errors.NoDefaultConstructor(type);
        return default!;
    }

    public static Delegate? GetConstructorDelegate(this Type type)
        => CtorDelegate0Cache.GetOrAdd(type, tObject => {
            var argTypes = Type.EmptyTypes;
            var ctor = tObject.GetConstructor(argTypes);
            if (ctor == null) return null;

            var eCtor = Expression.New(ctor, Array.Empty<Expression>());
            return Expression.Lambda(eCtor).Compile();
        });

    public static Delegate? GetConstructorDelegate(this Type type, Type argument1)
        => CtorDelegate1Cache.GetOrAdd((type, argument1), key => {
            var (tObject, tArg1) = key;
            var ctor = tObject.GetConstructor(new[] {tArg1});
            if (ctor == null) return null;

            var eArgs = new[] {
                Expression.Parameter(tArg1, "arg1"),
            };
            var eCtor = Expression.New(ctor, eArgs);
            return Expression.Lambda(eCtor, eArgs).Compile();
        });

    public static Delegate? GetConstructorDelegate(this Type type, Type argument1, Type argument2)
        => CtorDelegate2Cache.GetOrAdd((type, argument1, argument2), key => {
            var (tObject, tArg1, tArg2) = key;
            var ctor = tObject.GetConstructor(new[] {tArg1, tArg2});
            if (ctor == null) return null;

            var eArgs = new[] {
                Expression.Parameter(tArg1, "arg1"),
                Expression.Parameter(tArg2, "arg2"),
            };
            var eCtor = Expression.New(ctor, eArgs);
            return Expression.Lambda(eCtor, eArgs).Compile();
        });

    public static Delegate? GetConstructorDelegate(this Type type, Type argument1, Type argument2, Type argument3)
        => CtorDelegate3Cache.GetOrAdd((type, argument1, argument2, argument3), key => {
            var (tObject, tArg1, tArg2, tArg3) = key;
            var ctor = tObject.GetConstructor(new[] {tArg1, tArg2, tArg3});
            if (ctor == null) return null;

            var eArgs = new[] {
                Expression.Parameter(tArg1, "arg1"),
                Expression.Parameter(tArg2, "arg2"),
                Expression.Parameter(tArg3, "arg3"),
            };
            var eCtor = Expression.New(ctor, eArgs);
            return Expression.Lambda(eCtor, eArgs).Compile();
        });

    public static Delegate? GetConstructorDelegate(this Type type,
        Type argument1, Type argument2, Type argument3, Type argument4)
        => CtorDelegate4Cache.GetOrAdd(
            (type, argument1, argument2, argument3, argument4),
            key => {
                var (tObject, tArg1, tArg2, tArg3, tArg4) = key;
                var ctor = tObject.GetConstructor(new[] {tArg1, tArg2, tArg3, tArg4});
                if (ctor == null) return null;

                var eArgs = new[] {
                    Expression.Parameter(tArg1, "arg1"),
                    Expression.Parameter(tArg2, "arg2"),
                    Expression.Parameter(tArg3, "arg3"),
                    Expression.Parameter(tArg4, "arg4"),
                };
                var eCtor = Expression.New(ctor, eArgs);
                return Expression.Lambda(eCtor, eArgs).Compile();
            });

    public static Delegate? GetConstructorDelegate(this Type type,
        Type argument1, Type argument2, Type argument3, Type argument4, Type argument5)
        => CtorDelegate5Cache.GetOrAdd(
            (type, argument1, argument2, argument3, argument4, argument5),
            key => {
                var (tObject, tArg1, tArg2, tArg3, tArg4, tArg5) = key;
                var ctor = tObject.GetConstructor(new[] {tArg1, tArg2, tArg3, tArg4, tArg5});
                if (ctor == null) return null;

                var eArgs = new[] {
                    Expression.Parameter(tArg1, "arg1"),
                    Expression.Parameter(tArg2, "arg2"),
                    Expression.Parameter(tArg3, "arg3"),
                    Expression.Parameter(tArg4, "arg4"),
                    Expression.Parameter(tArg5, "arg5"),
                };
                var eCtor = Expression.New(ctor, eArgs);
                return Expression.Lambda(eCtor, eArgs).Compile();
            });

    public static Func<T1, object> GetConstructorDelegate<T1>(this Type type, T1 argument1)
        => (Func<T1, object>) type.GetConstructorDelegate(typeof(T1))!;
    public static Func<T1, T2, object> GetConstructorDelegate<T1, T2>(this Type type, T1 argument1, T2 argument2)
        => (Func<T1, T2, object>) type.GetConstructorDelegate(typeof(T1), typeof(T2))!;
    public static Func<T1, T2, T3, object> GetConstructorDelegate<T1, T2, T3>(this Type type, T1 argument1, T2 argument2, T3 argument3)
        => (Func<T1, T2, T3, object>) type.GetConstructorDelegate(typeof(T1), typeof(T2), typeof(T3))!;
    public static Func<T1, T2, T3, T4, object> GetConstructorDelegate<T1, T2, T3, T4>(this Type type, T1 argument1, T2 argument2, T3 argument3, T4 argument4)
        => (Func<T1, T2, T3, T4, object>) type.GetConstructorDelegate(typeof(T1), typeof(T2), typeof(T3), typeof(T4))!;
    public static Func<T1, T2, T3, T4, T5, object> GetConstructorDelegate<T1, T2, T3, T4, T5>(this Type type, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5)
        => (Func<T1, T2, T3, T4, T5, object>) type.GetConstructorDelegate(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5))!;

    public static object CreateInstance(this Type type)
    {
        var ctor = (Func<object>) type.GetConstructorDelegate()!;
        return ctor();
    }

    public static object CreateInstance<T1>(this Type type, T1 argument1)
    {
        var ctor = (Func<T1, object>) type.GetConstructorDelegate(typeof(T1))!;
        return ctor(argument1);
    }

    public static object CreateInstance<T1, T2>(this Type type, T1 argument1, T2 argument2)
    {
        var ctor = (Func<T1, T2, object>) type.GetConstructorDelegate(typeof(T1), typeof(T2))!;
        return ctor(argument1, argument2);
    }

    public static object CreateInstance<T1, T2, T3>(this Type type, T1 argument1, T2 argument2, T3 argument3)
    {
        var ctor = (Func<T1, T2, T3, object>) type.GetConstructorDelegate(typeof(T1), typeof(T2), typeof(T3))!;
        return ctor(argument1, argument2, argument3);
    }

    public static object CreateInstance<T1, T2, T3, T4>(this Type type, T1 argument1, T2 argument2, T3 argument3, T4 argument4)
    {
        var ctor = (Func<T1, T2, T3, T4, object>) type.GetConstructorDelegate(typeof(T1), typeof(T2), typeof(T3), typeof(T4))!;
        return ctor(argument1, argument2, argument3, argument4);
    }

    public static object CreateInstance<T1, T2, T3, T4, T5>(this Type type, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5)
    {
        var ctor = (Func<T1, T2, T3, T4, T5, object>) type.GetConstructorDelegate(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5))!;
        return ctor(argument1, argument2, argument3, argument4, argument5);
    }
}
