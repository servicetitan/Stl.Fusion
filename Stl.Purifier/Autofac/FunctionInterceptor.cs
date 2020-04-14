using System;
using System.Collections.Concurrent;
using System.CommandLine.Invocation;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Stl.Concurrency;
using Stl.Locking;

namespace Stl.Purifier.Autofac
{
    public class FunctionInterceptor : IInterceptor
    {
        private readonly MethodInfo _createTypedHandlerMethod;
        private readonly Func<MethodInfo, IInvocation, Action<IInvocation>?> _createHandler;
        private readonly ConcurrentDictionary<MethodInfo, Action<IInvocation>?> _handlers = 
            new ConcurrentDictionary<MethodInfo, Action<IInvocation>?>();

        protected ConcurrentIdGenerator<long> TagGenerator { get; }
        protected IComputedRegistry<(IFunction, ArrayKey)> ComputedRegistry { get; }
        protected IAsyncLockSet<(IFunction, ArrayKey)>? Locks { get; }                      

        public FunctionInterceptor(
            ConcurrentIdGenerator<long> tagGenerator,
            IComputedRegistry<(IFunction, ArrayKey)> computedRegistry,
            IAsyncLockSet<(IFunction, ArrayKey)>? locks = null) 
        {
            locks ??= new AsyncLockSet<(IFunction, ArrayKey)>(ReentryMode.CheckedFail);
            _createHandler = CreateHandler;
            _createTypedHandlerMethod = GetType()
                .GetMethods()
                .Single(m => m.Name == nameof(CreateTypedHandler));
            
            TagGenerator = tagGenerator;
            ComputedRegistry = computedRegistry;
            Locks = locks;
        }

        public void Intercept(IInvocation invocation)
        {
            var handler = _handlers.GetOrAdd(invocation.Method, _createHandler, invocation);
            if (handler == null)
                invocation.Proceed();
            else 
                handler.Invoke(invocation);
        }

        private Action<IInvocation>? CreateHandler(MethodInfo key, IInvocation initialInvocation)
        {
            var methodInfo = initialInvocation.GetConcreteMethodInvocationTarget();
            var extMethodInfo = ExtendedMethodInfo.Create(methodInfo);
            if (extMethodInfo == null)
                return null;

            return (Action<IInvocation>) _createTypedHandlerMethod
                .MakeGenericMethod(extMethodInfo.OutputType)
                .Invoke(this, new [] {(object) initialInvocation, extMethodInfo})!;
        }

        protected virtual Action<IInvocation> CreateTypedHandler<TOut>(
            IInvocation initialInvocation, ExtendedMethodInfo method)
        {
            var function = new InterceptedFunction<TOut>(method, TagGenerator, ComputedRegistry, Locks);
            return invocation => {
                // Preparing for invocation by processing invocation arguments:
                // - Put IInvocationProceedInfo there
                // - Get CancellationToken from there
                var method = function.Method;
                var arguments = invocation.Arguments;
                arguments[method.ProceedInfoArgumentIndex] = invocation.CaptureProceedInfo();
                var cancellationToken = CancellationToken.None;
                if (method.CancellationTokenArgumentIndex >= 0)
                    cancellationToken = (CancellationToken) arguments[method.CancellationTokenArgumentIndex];
                var usedBy = Computed.Current;

                // Invoking the function
                var key = new ArrayKey(arguments, method.UsedArgumentBitmap);
                var valueTask = function.InvokeAsync(key, usedBy, cancellationToken);

                if (invocation.ReturnValue != null)
                    // It's a real invocation (cache miss),
                    // so no extra work is needed
                    return;

                // No real invocation (cache hit), so we have to
                // set invocation.ReturnValue to the correct one. 
                if (method.ReturnsComputed) {
                    if (method.ReturnsValueTask)
                        // ReSharper disable once HeapView.BoxingAllocation
                        invocation.ReturnValue = valueTask;
                    else
                        invocation.ReturnValue = valueTask.AsTask();
                }
                else {
                    var strippedResultTask = valueTask.AsTask().ContinueWith(t => t.Result.Value, cancellationToken);
                    if (method.ReturnsValueTask)
                        // ReSharper disable once HeapView.BoxingAllocation
                        invocation.ReturnValue = new ValueTask<TOut>(strippedResultTask);
                    else
                        invocation.ReturnValue = strippedResultTask;
                }
            };
        }
    }
}
