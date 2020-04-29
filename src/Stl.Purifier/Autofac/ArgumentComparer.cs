using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Stl.Purifier.Autofac
{
    public class ArgumentComparer
    {
        public static readonly ArgumentComparer Default = new ArgumentComparer();
        public static readonly IgnoreArgumentComparer Ignore = new IgnoreArgumentComparer();
        public static readonly ArgumentComparer ByRef = new ByRefArgumentComparer();

        public Func<object, int> GetHashCodeFunc { get; protected set; } = 
            o => o?.GetHashCode() ?? 0;
        public Func<object, object, bool> EqualsFunc { get; protected set; } = 
            (objA, objB) => objA == objB || (objA?.Equals(objB) ?? false);
    }

    public abstract class EquatableArgumentComparer : ArgumentComparer
    {
        public bool IsAvailable { get; protected set; } = false;
    }

    public class EquatableArgumentComparer<T> : EquatableArgumentComparer
    {
        public static readonly EquatableArgumentComparer<T> Instance = 
            new EquatableArgumentComparer<T>();

        private EquatableArgumentComparer()
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
            GetHashCodeFunc = (Func<object, int>)
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
            EqualsFunc = (Func<object, object, bool>)
                Expression.Lambda(equalsBody, eSrc, eOther).Compile();

            IsAvailable = true;
        }
    }

    public class IgnoreArgumentComparer : ArgumentComparer
    {
        internal IgnoreArgumentComparer()
        {
            GetHashCodeFunc = _ => 0;
            EqualsFunc = (a, b) => true;
        }
    }

    public class ByRefArgumentComparer : ArgumentComparer
    {
        internal ByRefArgumentComparer()
        {
            GetHashCodeFunc = obj => obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
            EqualsFunc = ReferenceEquals;
        }
    }
}
