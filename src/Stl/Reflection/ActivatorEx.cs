using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Stl.Reflection
{
    public static class ActivatorEx
    {
        private static readonly ConcurrentDictionary<Type, Delegate?> CtorDelegate0Cache =
            new ConcurrentDictionary<Type, Delegate?>();
        private static readonly ConcurrentDictionary<(Type, Type), Delegate?> CtorDelegate1Cache =
            new ConcurrentDictionary<(Type, Type), Delegate?>();
        private static readonly ConcurrentDictionary<(Type, Type, Type), Delegate?> CtorDelegate2Cache =
            new ConcurrentDictionary<(Type, Type, Type), Delegate?>();
        private static readonly ConcurrentDictionary<(Type, Type, Type, Type), Delegate?> CtorDelegate3Cache =
            new ConcurrentDictionary<(Type, Type, Type, Type), Delegate?>();
        private static readonly ConcurrentDictionary<(Type, Type, Type, Type, Type), Delegate?> CtorDelegate4Cache =
            new ConcurrentDictionary<(Type, Type, Type, Type, Type), Delegate?>();
        private static readonly ConcurrentDictionary<(Type, Type, Type, Type, Type, Type), Delegate?> CtorDelegate5Cache =
            new ConcurrentDictionary<(Type, Type, Type, Type, Type, Type), Delegate?>();

        public static Delegate? GetConstructorDelegate(this Type type) 
            => CtorDelegate0Cache.GetOrAddChecked(type, tObject => {
                var argTypes = new Type[0];
                var ctor = tObject.GetConstructor(argTypes);
                if (ctor == null) return null;

                var eCtor = Expression.New(ctor, new Expression[0]);
                return Expression.Lambda(eCtor).Compile();
            });

        public static Delegate? GetConstructorDelegate(this Type type, Type argument1)
            => CtorDelegate1Cache.GetOrAddChecked((type, argument1), key => {
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
            => CtorDelegate2Cache.GetOrAddChecked((type, argument1, argument2), key => {
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
            => CtorDelegate3Cache.GetOrAddChecked((type, argument1, argument2, argument3), key => {
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
            => CtorDelegate4Cache.GetOrAddChecked(
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
            => CtorDelegate5Cache.GetOrAddChecked(
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

        public static object CreateInstance(this Type type)
        {
            var ctor = (Func<object>) type.GetConstructorDelegate()!;
            return ctor.Invoke();
        }

        public static object CreateInstance<T1>(this Type type, T1 argument1)
        {
            var ctor = (Func<T1, object>) type.GetConstructorDelegate(typeof(T1))!;
            return ctor.Invoke(argument1);
        }

        public static object CreateInstance<T1, T2>(this Type type, T1 argument1, T2 argument2)
        {
            var ctor = (Func<T1, T2, object>) type.GetConstructorDelegate(typeof(T1), typeof(T2))!;
            return ctor.Invoke(argument1, argument2);
        }

        public static object CreateInstance<T1, T2, T3>(this Type type, T1 argument1, T2 argument2, T3 argument3)
        {
            var ctor = (Func<T1, T2, T3, object>) type.GetConstructorDelegate(typeof(T1), typeof(T2), typeof(T3))!;
            return ctor.Invoke(argument1, argument2, argument3);
        }

        public static object CreateInstance<T1, T2, T3, T4>(this Type type, T1 argument1, T2 argument2, T3 argument3, T4 argument4)
        {
            var ctor = (Func<T1, T2, T3, T4, object>) type.GetConstructorDelegate(typeof(T1), typeof(T2), typeof(T3), typeof(T4))!;
            return ctor.Invoke(argument1, argument2, argument3, argument4);
        }

        public static object CreateInstance<T1, T2, T3, T4, T5>(this Type type, T1 argument1, T2 argument2, T3 argument3, T4 argument4, T5 argument5)
        {
            var ctor = (Func<T1, T2, T3, T4, T5, object>) type.GetConstructorDelegate(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5))!;
            return ctor.Invoke(argument1, argument2, argument3, argument4, argument5);
        }
    }
}
