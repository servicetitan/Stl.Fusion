using System.Linq.Expressions;

namespace Stl.Fusion.Interception;

public class EquatableArgumentHandler<T> : ArgumentHandler
{
    public static EquatableArgumentHandler<T> Instance { get; } = new();

    protected EquatableArgumentHandler()
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
    }
}
