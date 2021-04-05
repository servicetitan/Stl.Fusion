using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.DependencyInjection;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Interception
{
    public abstract class ComputeMethodInterceptorBase : InterceptorBase
    {
        // ReSharper disable once HeapView.BoxingAllocation
        private static readonly object NoCancellationTokenBoxed = CancellationToken.None;

        public new class Options : InterceptorBase.Options
        {
            public IComputedOptionsProvider? ComputedOptionsProvider { get; set; } = null!;
            public IArgumentHandlerProvider? ArgumentHandlerProvider { get; set; } = null!;
        }

        public IComputedOptionsProvider ComputedOptionsProvider { get; }
        public IArgumentHandlerProvider ArgumentHandlerProvider { get; }

        protected ComputeMethodInterceptorBase(
            Options options,
            IServiceProvider services,
            ILoggerFactory? loggerFactory = null)
            : base(options, services, loggerFactory)
        {
            ComputedOptionsProvider = options.ComputedOptionsProvider
                ?? services.GetRequiredService<IComputedOptionsProvider>();
            ArgumentHandlerProvider = options.ArgumentHandlerProvider
                ?? services.GetRequiredService<IArgumentHandlerProvider>();
        }

        protected override Action<IInvocation> CreateHandler<T>(
            IInvocation initialInvocation, MethodDef methodDef)
        {
            var computeMethodDef = (ComputeMethodDef) methodDef;
            var function = CreateFunction<T>(computeMethodDef);
            return invocation => {
                var input = computeMethodDef.CreateInput(function, (AbstractInvocation) invocation);
                var arguments = input.Arguments;
                var cancellationTokenIndex = computeMethodDef.CancellationTokenArgumentIndex;
                var cancellationToken = cancellationTokenIndex >= 0
                    ? (CancellationToken) arguments[cancellationTokenIndex]
                    : default;

                // Invoking the function
                var usedBy = Computed.GetCurrent();

                // InvokeAndStrip allows to get rid of one extra allocation
                // of a task stripping the result of regular Invoke.
                var task = function.InvokeAndStrip(input, usedBy, null, cancellationToken);
                if (cancellationTokenIndex >= 0)
                    // We don't want memory leaks + unexpected cancellation later
                    arguments[cancellationTokenIndex] = NoCancellationTokenBoxed;

                if (methodDef.ReturnsValueTask)
                    // ReSharper disable once HeapView.BoxingAllocation
                    invocation.ReturnValue = new ValueTask<T>(task);
                else
                    invocation.ReturnValue = task;
            };
        }

        protected abstract ComputeFunctionBase<T> CreateFunction<T>(ComputeMethodDef method);

        protected override MethodDef? CreateMethodDef(MethodInfo methodInfo, IInvocation initialInvocation)
        {
            ValidateType(initialInvocation.TargetType);

            var options = ComputedOptionsProvider.GetComputedOptions(methodInfo);
            if (options == null)
                return null;

            var methodDef = (ComputeMethodDef) Services.Activate(
                options.ComputeMethodDefType, this, methodInfo);
            return methodDef.IsValid ? methodDef : null;
        }
    }
}
