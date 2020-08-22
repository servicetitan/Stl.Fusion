using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Concurrency;
using Stl.DependencyInjection;
using Stl.Fusion.Swapping;
using Stl.Fusion.Interception.Internal;
using Stl.Reflection;
using Stl.Time;

namespace Stl.Fusion.Interception
{
    public abstract class InterceptorBase : IInterceptor
    {
        public class Options
        {
            public IArgumentHandlerProvider ArgumentHandlerProvider { get; set; } =
                Interception.ArgumentHandlerProvider.Default;
            public LogLevel LogLevel { get; set; } = LogLevel.Debug;
            public LogLevel ValidationLogLevel { get; set; } = LogLevel.Information;
        }

        private readonly MethodInfo _createTypedHandlerMethod;
        private readonly Func<MethodInfo, IInvocation, Action<IInvocation>?> _createHandler;
        private readonly Func<MethodInfo, IInvocation, InterceptedMethod?> _createInterceptedMethod;
        private readonly ConcurrentDictionary<MethodInfo, InterceptedMethod?> _interceptedMethodCache =
            new ConcurrentDictionary<MethodInfo, InterceptedMethod?>();
        private readonly ConcurrentDictionary<MethodInfo, Action<IInvocation>?> _handlerCache =
            new ConcurrentDictionary<MethodInfo, Action<IInvocation>?>();
        private readonly ConcurrentDictionary<Type, Unit> _validateTypeCache =
            new ConcurrentDictionary<Type, Unit>();

        protected ILoggerFactory LoggerFactory { get; }
        protected ILogger Log { get; }
        protected LogLevel LogLevel { get; }
        protected LogLevel ValidationLogLevel { get; }
        protected IServiceProvider Services { get; }
        protected IArgumentHandlerProvider ArgumentHandlerProvider { get; }

        protected InterceptorBase(
            Options options,
            IServiceProvider services,
            ILoggerFactory? loggerFactory = null)
        {
            LoggerFactory = loggerFactory ??= NullLoggerFactory.Instance;
            Log = LoggerFactory.CreateLogger(GetType());
            LogLevel = options.LogLevel;
            ValidationLogLevel = options.ValidationLogLevel;
            Services = services;

            ArgumentHandlerProvider = options.ArgumentHandlerProvider;

            _createHandler = CreateHandler;
            _createInterceptedMethod = CreateInterceptedMethod;
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

        public void ValidateType(Type type)
        {
            _validateTypeCache.GetOrAddChecked(type, (type1, self) => {
                self.ValidateTypeInternal(type1);
                return default;
            }, this);
        }

        protected virtual Action<IInvocation>? CreateHandler(MethodInfo methodInfo, IInvocation initialInvocation)
        {
            var proxyMethodInfo = initialInvocation.MethodInvocationTarget;
            var method = _interceptedMethodCache.GetOrAddChecked(proxyMethodInfo, _createInterceptedMethod, initialInvocation);
            if (method == null)
                return null;

            return (Action<IInvocation>) _createTypedHandlerMethod
                .MakeGenericMethod(method.OutputType)
                .Invoke(this, new object[] {initialInvocation, method})!;
        }

        protected virtual Action<IInvocation> CreateTypedHandler<T>(
            IInvocation initialInvocation, InterceptedMethod method)
        {
            var function = CreateFunction<T>(method);
            return invocation => {
                // ReSharper disable once VariableHidesOuterVariable
                var method = function.Method;
                var input = new InterceptedInput(function, method, invocation);

                // Invoking the function
                var cancellationToken = input.CancellationToken;
                var usedBy = Computed.GetCurrent();

                // InvokeAndStripAsync allows to get rid of one extra allocation
                // of a task stripping the result of regular InvokeAsync.
                var task = function.InvokeAndStripAsync(input, usedBy, null, cancellationToken);
                if (method.ReturnsValueTask)
                    // ReSharper disable once HeapView.BoxingAllocation
                    invocation.ReturnValue = new ValueTask<T>(task);
                else
                    invocation.ReturnValue = task;
            };
        }

        protected abstract InterceptedFunctionBase<T> CreateFunction<T>(InterceptedMethod method);

        protected virtual InterceptedMethod? CreateInterceptedMethod(MethodInfo proxyMethodInfo, IInvocation invocation)
        {
            ValidateType(invocation.TargetType);

            // We need an attribute from interface / original type, so we
            // can't use proxyMethodInfo here, b/c it points to a proxy method.
            var attr = GetInterceptedMethodAttribute(proxyMethodInfo);
            if (attr == null)
                // No attribute -> no interception.
                // GetInterceptedMethodAttribute can provide a default though.
                return null;

            var returnType = proxyMethodInfo.ReturnType;
            if (!returnType.IsGenericType)
                return null;

            var returnTypeGtd = returnType.GetGenericTypeDefinition();
            var returnsTask = returnTypeGtd == typeof(Task<>);
            var returnsValueTask = returnTypeGtd == typeof(ValueTask<>);
            if (!(returnsTask || returnsValueTask))
                return null;

            var outputType = returnType.GetGenericArguments()[0];
            var invocationTargetType = proxyMethodInfo.ReflectedType;
            var options = ComputedOptions.FromAttribute(attr, GetCacheAttribute(proxyMethodInfo));
            var parameters = proxyMethodInfo.GetParameters();
            var r = new InterceptedMethod {
                MethodInfo = proxyMethodInfo,
                Attribute = attr,
                OutputType = outputType,
                ReturnsValueTask = returnsValueTask,
                InvocationTargetHandler = ArgumentHandlerProvider.GetInvocationTargetHandler(
                    proxyMethodInfo, invocationTargetType!),
                ArgumentHandlers = new ArgumentHandler[parameters.Length],
                Options = options,
            };

            for (var i = 0; i < parameters.Length; i++) {
                var p = parameters[i];
                r.ArgumentHandlers[i] = ArgumentHandlerProvider.GetArgumentHandler(proxyMethodInfo, p);
                var parameterType = p.ParameterType;
                if (typeof(CancellationToken).IsAssignableFrom(parameterType))
                    r.CancellationTokenArgumentIndex = i;
            }

            return r;
        }

        protected virtual InterceptedMethodAttribute? GetInterceptedMethodAttribute(MethodInfo method)
            => method.GetAttribute<InterceptedMethodAttribute>(true, true);

        protected virtual SwapAttribute? GetCacheAttribute(MethodInfo method)
            => method.GetAttribute<SwapAttribute>(true, true);

        protected abstract void ValidateTypeInternal(Type type);
    }
}
