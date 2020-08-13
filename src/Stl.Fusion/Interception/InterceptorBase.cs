using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Concurrency;
using Stl.Fusion.Interception.Internal;
using Stl.Time;

namespace Stl.Fusion.Interception
{
    public abstract class InterceptorBase : IInterceptor
    {
        public class Options
        {
            public IArgumentComparerProvider ArgumentComparerProvider { get; set; } =
                Interception.ArgumentComparerProvider.Default;
            public IMomentClock Clock { get; set; } = CoarseCpuClock.Instance;
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
        protected IComputedRegistry Registry { get; }
        protected IArgumentComparerProvider ArgumentComparerProvider { get; }
        protected bool RequiresAttribute { get; set; } = true;

        protected InterceptorBase(
            Options options,
            IComputedRegistry? registry = null,
            ILoggerFactory? loggerFactory = null)
        {
            LoggerFactory = loggerFactory ??= NullLoggerFactory.Instance;
            Log = LoggerFactory.CreateLogger(GetType());
            LogLevel = options.LogLevel;
            ValidationLogLevel = options.ValidationLogLevel;

            Registry = registry ?? ComputedRegistry.Default;
            ArgumentComparerProvider = options.ArgumentComparerProvider;

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
            var proxyMethodInfo = initialInvocation.GetConcreteMethodInvocationTarget();
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
            var attrs = invocation.Method
                .GetCustomAttributes(typeof(InterceptedMethodAttribute), true)
                .Cast<InterceptedMethodAttribute>()
                .ToArray();
            if (attrs.Any(a => !a.IsEnabled))
                // Explicitly disabled -> no interception
                return null;
            if (RequiresAttribute && attrs.Length == 0)
                // No attribute while it is required -> no interception
                return null;
            var attr = attrs.FirstOrDefault();

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
            var options = new ComputedOptions(
                GetTimespan<ComputeMethodAttribute>(attr, a => a.KeepAliveTime),
                GetTimespan<ComputeMethodAttribute>(attr, a => a.ErrorAutoInvalidateTime),
                GetTimespan<ComputeMethodAttribute>(attr, a => a.AutoInvalidateTime));
            var parameters = proxyMethodInfo.GetParameters();
            var r = new InterceptedMethod {
                MethodInfo = proxyMethodInfo,
                OutputType = outputType,
                ReturnsValueTask = returnsValueTask,
                InvocationTargetComparer = ArgumentComparerProvider.GetInvocationTargetComparer(
                    proxyMethodInfo, invocationTargetType!),
                ArgumentComparers = new ArgumentComparer[parameters.Length],
                Options = options,
            };

            for (var i = 0; i < parameters.Length; i++) {
                var p = parameters[i];
                r.ArgumentComparers[i] = ArgumentComparerProvider.GetArgumentComparer(proxyMethodInfo, p);
                var parameterType = p.ParameterType;
                if (typeof(CancellationToken).IsAssignableFrom(parameterType))
                    r.CancellationTokenArgumentIndex = i;
            }

            return r;
        }

        protected abstract void ValidateTypeInternal(Type type);

        // Private methods

        private TimeSpan? GetTimespan<TAttribute>(Attribute attr, Func<TAttribute, double> propertyGetter)
        {
            if (!(attr is TAttribute typedAttr))
                return null;
            var value = propertyGetter.Invoke(typedAttr);
            if (double.IsNaN(value))
                return null;
            if (double.IsPositiveInfinity(value))
                return TimeSpan.MaxValue;
            return TimeSpan.FromSeconds(value);
        }
    }
}
