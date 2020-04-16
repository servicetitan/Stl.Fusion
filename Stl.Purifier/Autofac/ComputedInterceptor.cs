using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Stl.Async;
using Stl.Concurrency;
using Stl.Locking;

namespace Stl.Purifier.Autofac
{
    public class ComputedInterceptor : IInterceptor
    {
        private readonly MethodInfo _createTypedHandlerMethod;
        private readonly Func<MethodInfo, IInvocation, Action<IInvocation>?> _createHandler;
        private readonly Func<MethodInfo, InterceptedMethod?> _createInterceptedMethod;
        private readonly ConcurrentDictionary<MethodInfo, InterceptedMethod?> _interceptedMethodCache = 
            new ConcurrentDictionary<MethodInfo, InterceptedMethod?>();
        private readonly ConcurrentDictionary<MethodInfo, Action<IInvocation>?> _handlerCache = 
            new ConcurrentDictionary<MethodInfo, Action<IInvocation>?>();

        protected ConcurrentIdGenerator<long> TagGenerator { get; }
        protected IComputedRegistry<(IFunction, InterceptedInput)> ComputedRegistry { get; }
        protected IArgumentComparerProvider ArgumentComparerProvider { get; }
        protected IAsyncLockSet<(IFunction, InterceptedInput)>? Locks { get; }                      

        public ComputedInterceptor(
            ConcurrentIdGenerator<long> tagGenerator,
            IComputedRegistry<(IFunction, InterceptedInput)> computedRegistry,
            IArgumentComparerProvider? argumentComparerProvider = null,
            IAsyncLockSet<(IFunction, InterceptedInput)>? locks = null) 
        {
            locks ??= new AsyncLockSet<(IFunction, InterceptedInput)>(ReentryMode.CheckedFail);
            argumentComparerProvider ??= Autofac.ArgumentComparerProvider.Default;

            _createHandler = CreateHandler;
            _createInterceptedMethod = CreateInterceptedMethod;
            _createTypedHandlerMethod = GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(m => m.Name == nameof(CreateTypedHandler));
            
            TagGenerator = tagGenerator;
            ComputedRegistry = computedRegistry;
            ArgumentComparerProvider = argumentComparerProvider;
            Locks = locks;
        }

        public void Intercept(IInvocation invocation)
        {
            var handler = _handlerCache.GetOrAdd(invocation.Method, _createHandler, invocation);
            if (handler == null)
                invocation.Proceed();
            else 
                handler.Invoke(invocation);
        }

        private Action<IInvocation>? CreateHandler(MethodInfo key, IInvocation initialInvocation)
        {
            var methodInfo = initialInvocation.GetConcreteMethodInvocationTarget();
            var method = _interceptedMethodCache.GetOrAdd(methodInfo, _createInterceptedMethod);
            if (method == null)
                return null;

            return (Action<IInvocation>) _createTypedHandlerMethod
                .MakeGenericMethod(method.OutputType)
                .Invoke(this, new [] {(object) initialInvocation, method})!;
        }

        protected virtual Action<IInvocation> CreateTypedHandler<TOut>(
            IInvocation initialInvocation, InterceptedMethod method)
        {
            var function = new InterceptedFunction<TOut>(method, TagGenerator, ComputedRegistry, Locks);
            return invocation => {
                // ReSharper disable once VariableHidesOuterVariable
                var method = function.Method;
                var input = new InterceptedInput(method, invocation);
                var callOptions = input.CallOptions;

                // Special flow: CallOptions.CachedOnly & Invalidate 
                if ((callOptions & CallOptions.CachedOnly) != 0) {
                    var computed = function.TryGetCached(input);
                    if ((callOptions & CallOptions.Invalidate) == CallOptions.Invalidate)
                        computed?.Invalidate();
                    if (method.ReturnsComputed) {
                        if (method.ReturnsValueTask)
                            // ReSharper disable once HeapView.BoxingAllocation
                            invocation.ReturnValue = ValueTaskEx.FromResult(computed);
                        else
                            invocation.ReturnValue = Task.FromResult(computed);
                    }
                    else {
                        var value = computed != null ? computed.UnsafeValue : default;
                        if (method.ReturnsValueTask)
                            // ReSharper disable once HeapView.BoxingAllocation
                            invocation.ReturnValue = ValueTaskEx.FromResult(value!);
                        else
                            invocation.ReturnValue = Task.FromResult(value);
                    }
                    return;
                }

                // Invoking the function
                var cancellationToken = input.CancellationToken;
                var usedBy = Computed.Current();
                var valueTask = function.InvokeAsync(input, usedBy, cancellationToken);

                if (invocation.ReturnValue != null)
                    // It's a real invocation (cache miss),
                    // + likely, a complete evaluation of the async flow,
                    // i.e. a very rare case when no any extra work is needed. 
                    return;

                // Either no real invocation (cache hit),
                // or activation of async flow (e.g. on async lock inside
                // InvokeAsync). We don't know the result here yet,
                // but we have a ValueTask<IComputed<TOut>> allowing us 
                // to set invocation.ReturnValue to the correct one.
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

        protected virtual InterceptedMethod? CreateInterceptedMethod(MethodInfo methodInfo)
        {
            var attribute = methodInfo.GetCustomAttribute<ComputedAttribute>(true);
            var isEnabled = attribute?.IsEnabled ?? true;
            if (!isEnabled)
                return null;

            var returnType = methodInfo.ReturnType;
            if (!returnType.IsGenericType)
                return null;

            var returnTypeGtd = returnType.GetGenericTypeDefinition();
            var returnsTask = returnTypeGtd == typeof(Task<>);
            var returnsValueTask = returnTypeGtd == typeof(ValueTask<>);
            if (!(returnsTask || returnsValueTask))
                return null;

            var outputType = returnType.GetGenericArguments()[0];
            var returnsComputed = false;
            if (outputType.IsGenericType) {
                var returnTypeArgGtd = outputType.GetGenericTypeDefinition();
                if (returnTypeArgGtd == typeof(IComputed<>)) {
                    returnsComputed = true;
                    outputType = outputType.GetGenericArguments()[0];
                }
            }

            var parameters = methodInfo.GetParameters();
            var r = new InterceptedMethod {
                MethodInfo = methodInfo,
                OutputType = outputType,
                ReturnsValueTask = returnsValueTask,
                ReturnsComputed = returnsComputed,
                ArgumentComparers = new ArgumentComparer[parameters.Length],
            };

            for (var i = 0; i < parameters.Length; i++) {
                var p = parameters[i];
                r.ArgumentComparers[i] = ArgumentComparerProvider.GetComparer(methodInfo, p);
                var parameterType = p.ParameterType;
                if (typeof(CancellationToken).IsAssignableFrom(parameterType))
                    r.CancellationTokenArgumentIndex = i;
                if (typeof(CallOptions).IsAssignableFrom(parameterType))
                    r.CallOptionsArgumentIndex = i;
            }

            return r;
        }

    }
}
