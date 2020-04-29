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

        public static Delegate? GetConstructorDelegate(this Type type) 
            => CtorDelegate0Cache.GetOrAdd(type, tObject => {
                var argTypes = new Type[0];
                var ctor = tObject.GetConstructor(argTypes);
                if (ctor == null) return null;

                var eCtor = Expression.New(ctor, new Expression[0]);
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
    }
}
