using System;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Stl.Interception.Internal
{
    public abstract class MethodDef
    {
        public IInterceptor Interceptor { get; }
        public MethodInfo MethodInfo { get; }
        public bool IsAsyncMethod { get; protected init; } = false;
        public bool ReturnsTask { get; protected init; } = false;
        public bool ReturnsValueTask { get; protected init; } = false;
        public Type UnwrappedReturnType { get; protected init; } = null!;
        public bool IsValid { get; protected init; } = false;

        protected MethodDef(
            IInterceptor interceptor,
            MethodInfo methodInfo)
        {
            Interceptor = interceptor;
            MethodInfo = methodInfo;

            var returnType = methodInfo.ReturnType;
            if (!returnType.IsGenericType) {
                UnwrappedReturnType = returnType;
                return;
            }

            var returnTypeGtd = returnType.GetGenericTypeDefinition();
            ReturnsTask = returnTypeGtd == typeof(Task<>);
            ReturnsValueTask = returnTypeGtd == typeof(ValueTask<>);
            IsAsyncMethod = ReturnsTask || ReturnsValueTask;
            if (IsAsyncMethod)
                UnwrappedReturnType = returnType.GetGenericArguments()[0];
        }
    }
}
