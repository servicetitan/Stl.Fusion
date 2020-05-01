using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Castle.DynamicProxy;

namespace Stl.Fusion.Autofac.Internal
{
    public static class InvocationProceedInfoEx
    {
        private static readonly Func<IInvocationProceedInfo, IInvocation> InvocationReader;

        static InvocationProceedInfoEx()
        {
            var aiType = typeof(AbstractInvocation);
            var ipiType = typeof(IInvocationProceedInfo);
            var piType = aiType.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(t => ipiType.IsAssignableFrom(t));
            var eSrc = Expression.Parameter(typeof(IInvocationProceedInfo), "source");
            var body = Expression.Field(Expression.TypeAs(eSrc, piType), "invocation"); 
            InvocationReader = (Func<IInvocationProceedInfo, IInvocation>)
                Expression.Lambda(body, eSrc).Compile();
        }

        public static IInvocation GetInvocation(this IInvocationProceedInfo source)
            => InvocationReader.Invoke(source);
    }
}
