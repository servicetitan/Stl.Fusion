using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Castle.DynamicProxy;

namespace Stl.Fusion.Interception.Internal
{
    public static class AbstractInvocationExt
    {
        private static readonly Func<AbstractInvocation, int> GetCurrentInterceptorIndexFunc;
        private static readonly Action<AbstractInvocation, int> SetCurrentInterceptorIndexFunc;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCurrentInterceptorIndex(this AbstractInvocation invocation)
            => GetCurrentInterceptorIndexFunc.Invoke(invocation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCurrentInterceptorIndex(this AbstractInvocation invocation, int value)
            => SetCurrentInterceptorIndexFunc.Invoke(invocation, value);

        static AbstractInvocationExt()
        {
            var tAbstractInvocation = typeof(AbstractInvocation);
            var fInterceptorIndex = tAbstractInvocation.GetField(
                "currentInterceptorIndex", BindingFlags.Instance | BindingFlags.NonPublic);
            var pTarget = Expression.Parameter(tAbstractInvocation, "target");
            var pValue = Expression.Parameter(typeof(int), "value");
            GetCurrentInterceptorIndexFunc = Expression
                .Lambda<Func<AbstractInvocation, int>>(
                    Expression.Field(pTarget, fInterceptorIndex!),
                    pTarget)
                .Compile();
            SetCurrentInterceptorIndexFunc = Expression
                .Lambda<Action<AbstractInvocation, int>>(
                    Expression.Assign(Expression.Field(pTarget, fInterceptorIndex!), pValue),
                    pTarget, pValue)
                .Compile();
        }
    }
}
