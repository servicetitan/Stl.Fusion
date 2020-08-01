using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using Stl.Concurrency;

namespace Stl.Fusion.Internal
{
    public class InterfaceCastInterceptor : IInterceptor
    {
        private readonly MethodInfo _createTypedHandlerMethod;
        private readonly Func<MethodInfo, IInvocation, Action<IInvocation>?> _createHandler;
        private readonly ConcurrentDictionary<MethodInfo, Action<IInvocation>?> _handlerCache =
            new ConcurrentDictionary<MethodInfo, Action<IInvocation>?>();

        public InterfaceCastInterceptor()
        {
            _createHandler = CreateHandler;
            _createTypedHandlerMethod = GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(m => m.Name == nameof(CreateTypedHandler));
        }

        public void Intercept(IInvocation invocation)
        {
            var handler = _handlerCache.GetOrAddChecked(invocation.Method, _createHandler, invocation);
            if (handler == null)
                invocation.Proceed();
            else
                handler.Invoke(invocation);
        }

        private Action<IInvocation>? CreateHandler(MethodInfo methodInfo, IInvocation initialInvocation)
        {
            return (Action<IInvocation>) _createTypedHandlerMethod
                .MakeGenericMethod(initialInvocation.Method.ReturnType)
                .Invoke(this, new [] {(object) initialInvocation})!;
        }

        protected virtual Action<IInvocation> CreateTypedHandler<T>(IInvocation initialInvocation)
        {
            var tProxy = initialInvocation.Proxy.GetType();
            var tTarget = initialInvocation.TargetType;
            var mSource = initialInvocation.Method;
            var mArgTypes = mSource.GetParameters().Select(p => p.ParameterType).ToArray();
            var mTarget = tTarget.GetMethod(mSource.Name, mArgTypes);
            var fTarget = tProxy.GetField("__target", BindingFlags.Instance | BindingFlags.NonPublic);
            return invocation => {
                // TODO: Get rid of reflection here (not critical)
                var target = fTarget.GetValue(invocation.Proxy);
                invocation.ReturnValue = mTarget.Invoke(target, invocation.Arguments);
            };
        }
    }
}
