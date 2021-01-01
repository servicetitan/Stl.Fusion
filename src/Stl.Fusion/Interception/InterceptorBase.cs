using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Concurrency;
using Stl.DependencyInjection;
using Stl.Reflection;

namespace Stl.Fusion.Interception
{
    public abstract class InterceptorBase : IInterceptor, IHasServiceProvider
    {
        // ReSharper disable once HeapView.BoxingAllocation
        private static readonly object NoCancellationTokenBoxed = CancellationToken.None;

        public class Options
        {
            public IComputedOptionsProvider? ComputedOptionsProvider { get; set; } = null!;
            public IArgumentHandlerProvider? ArgumentHandlerProvider { get; set; } = null!;
            public LogLevel LogLevel { get; set; } = LogLevel.Debug;
            public LogLevel ValidationLogLevel { get; set; } = LogLevel.Information;
        }

        private readonly MethodInfo _createTypedHandlerMethod;
        private readonly Func<MethodInfo, IInvocation, Action<IInvocation>?> _createHandler;
        private readonly Func<MethodInfo, IInvocation, InterceptedMethodDescriptor?> _createInterceptedMethod;
        private readonly ConcurrentDictionary<MethodInfo, InterceptedMethodDescriptor?> _interceptedMethodCache = new();
        private readonly ConcurrentDictionary<MethodInfo, Action<IInvocation>?> _handlerCache = new();
        private readonly ConcurrentDictionary<Type, Unit> _validateTypeCache = new();

        protected ILoggerFactory LoggerFactory { get; }
        protected ILogger Log { get; }
        protected LogLevel LogLevel { get; }
        protected LogLevel ValidationLogLevel { get; }

        public IServiceProvider ServiceProvider { get; }
        public IComputedOptionsProvider ComputedOptionsProvider { get; }
        public IArgumentHandlerProvider ArgumentHandlerProvider { get; }

        protected InterceptorBase(
            Options options,
            IServiceProvider serviceProvider,
            ILoggerFactory? loggerFactory = null)
        {
            LoggerFactory = loggerFactory ??= NullLoggerFactory.Instance;
            Log = LoggerFactory.CreateLogger(GetType());
            LogLevel = options.LogLevel;
            ValidationLogLevel = options.ValidationLogLevel;
            ServiceProvider = serviceProvider;
            ComputedOptionsProvider = options.ComputedOptionsProvider
                ?? serviceProvider.GetRequiredService<IComputedOptionsProvider>();
            ArgumentHandlerProvider = options.ArgumentHandlerProvider
                ?? serviceProvider.GetRequiredService<IArgumentHandlerProvider>();

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
            IInvocation initialInvocation, InterceptedMethodDescriptor method)
        {
            var function = CreateFunction<T>(method);
            return invocation => {
                var input = method.CreateInput(function, (AbstractInvocation) invocation);
                var arguments = input.Arguments;
                var cancellationTokenIndex = method.CancellationTokenArgumentIndex;
                var cancellationToken = cancellationTokenIndex >= 0
                    ? (CancellationToken) arguments[cancellationTokenIndex]
                    : default;

                // Invoking the function
                var usedBy = Computed.GetCurrent();

                // InvokeAndStripAsync allows to get rid of one extra allocation
                // of a task stripping the result of regular InvokeAsync.
                var task = function.InvokeAndStripAsync(input, usedBy, null, cancellationToken);
                if (cancellationTokenIndex >= 0)
                    // We don't want memory leaks + unexpected cancellation later
                    arguments[cancellationTokenIndex] = NoCancellationTokenBoxed;

                if (method.ReturnsValueTask)
                    // ReSharper disable once HeapView.BoxingAllocation
                    invocation.ReturnValue = new ValueTask<T>(task);
                else
                    invocation.ReturnValue = task;
            };
        }

        protected abstract InterceptedFunctionBase<T> CreateFunction<T>(InterceptedMethodDescriptor method);

        protected virtual InterceptedMethodDescriptor? CreateInterceptedMethod(MethodInfo methodInfo, IInvocation initialInvocation)
        {
            ValidateType(initialInvocation.TargetType);

            var options = ComputedOptionsProvider.GetComputedOptions(this, methodInfo);
            if (options == null)
                return null;

            var interceptedMethod = (InterceptedMethodDescriptor) ServiceProvider.Activate(
                options.InterceptedMethodDescriptorType, this, methodInfo);
            return interceptedMethod.IsValid ? interceptedMethod : null;
        }

        protected abstract void ValidateTypeInternal(Type type);

        protected virtual InterceptedMethodAttribute? GetInterceptedMethodAttribute(MethodInfo method)
            => method.GetAttribute<InterceptedMethodAttribute>(true, true);

        protected virtual SwapAttribute? GetSwapAttribute(MethodInfo method)
            => method.GetAttribute<SwapAttribute>(true, true);
    }
}
