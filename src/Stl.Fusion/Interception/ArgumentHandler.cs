using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using Stl.Extensibility;

namespace Stl.Fusion.Interception
{
    public class ArgumentHandler
    {
        public static ArgumentHandler Default { get; } = new ArgumentHandler();

        public Func<object?, int> GetHashCodeFunc { get; protected set; } =
            o => o?.GetHashCode() ?? 0;
        public Func<object?, object?, bool> EqualsFunc { get; protected set; } =
            (objA, objB) => objA == objB || (objA?.Equals(objB) ?? false);
        public Func<object?, string> ToStringFunc { get; protected set; } =
            o => o?.ToString() ?? "␀";
    }

    public abstract class EquatableArgumentHandler : ArgumentHandler
    {
        public bool IsAvailable { get; protected set; } = false;
    }

    public class EquatableArgumentHandler<T> : EquatableArgumentHandler
    {
        public static EquatableArgumentHandler<T> Instance { get; } = new EquatableArgumentHandler<T>();

        private EquatableArgumentHandler()
        {
            var tType = typeof(T);
            var tEq = tType.GetInterfaces().SingleOrDefault(t => t == typeof(IEquatable<T>));
            if (tEq == null)
                return;

            var mGetHashCode = tType
                .GetMethods()
                .Single(m => m.Name == nameof(GetHashCode)
                    && m.IsVirtual
                    && m.ReturnType == typeof(int)
                    && m.GetParameters().Length == 0);
            var tEqMap = tType.GetInterfaceMap(tEq);
            var mEqualsIndex = tEqMap.InterfaceMethods
                .Select((mi, index) => (mi, index))
                .Single(p => p.mi.Name == nameof(Equals)).index;
            var mEquals = tEqMap.TargetMethods[mEqualsIndex];

            var eNull = Expression.Constant(null);
            var eSrc = Expression.Parameter(typeof(object), "source");
            var ghcBody = Expression.Condition(
                    Expression.ReferenceEqual(eSrc, eNull),
                    Expression.Constant(0),
                    Expression.Call(
                        Expression.ConvertChecked(eSrc, tType),
                        mGetHashCode)
                );
            GetHashCodeFunc = (Func<object?, int>)
                Expression.Lambda(ghcBody, eSrc).Compile();

            var eOther = Expression.Parameter(typeof(object), "other");
            var eTrue = Expression.Constant(true);
            var eFalse = Expression.Constant(false);
            var equalsBody = Expression.Condition(
                Expression.ReferenceEqual(eSrc, eNull),
                Expression.Condition(Expression.ReferenceEqual(eOther, eNull), eTrue, eFalse),
                Expression.Call(
                    Expression.ConvertChecked(eSrc, tType),
                    mEquals,
                    Expression.ConvertChecked(eOther, tType))
            );
            EqualsFunc = (Func<object?, object?, bool>)
                Expression.Lambda(equalsBody, eSrc, eOther).Compile();

            IsAvailable = true;
        }
    }

    [MatchFor(typeof(CancellationToken), typeof(ArgumentHandlerProvider))]
    public class IgnoreArgumentHandler : ArgumentHandler
    {
        public static IgnoreArgumentHandler Instance { get; } = new IgnoreArgumentHandler();

        private IgnoreArgumentHandler()
        {
            GetHashCodeFunc = _ => 0;
            EqualsFunc = (a, b) => true;
        }
    }

    public class ByRefArgumentHandler : ArgumentHandler
    {
        public static ByRefArgumentHandler Instance { get; } = new ByRefArgumentHandler();

        private ByRefArgumentHandler()
        {
            GetHashCodeFunc = obj => obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
            EqualsFunc = ReferenceEquals;
        }
    }

    [MatchFor(typeof(IHasId<>), typeof(ArgumentHandlerProvider))]
    public class HasIdArgumentHandler<T> : ArgumentHandler
    {
        public static HasIdArgumentHandler<T> Instance { get; } = new HasIdArgumentHandler<T>();

        private HasIdArgumentHandler()
        {
            GetHashCodeFunc = obj => {
                var hasId = (IHasId<T>?) obj;
                return hasId == null ? 0 : EqualityComparer<T>.Default.GetHashCode(hasId.Id);
            };
            EqualsFunc = (a, b) => {
                if (a == null)
                    return b == null;
                if (b == null)
                    return false;
                var aHasId = (IHasId<T>) a;
                var bHasId = (IHasId<T>) b;
                return EqualityComparer<T>.Default.Equals(aHasId.Id, bHasId.Id);
            };
        }
    }
}
