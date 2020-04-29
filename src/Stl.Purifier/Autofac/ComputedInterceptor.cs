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
using Stl.Purifier.Internal;
using Stl.Reflection;
using Stl.Time;

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

        protected ConcurrentIdGenerator<int> TagGenerator { get; }
        protected IComputedRegistry<(IFunction, InterceptedInput)> ComputedRegistry { get; }
        protected IArgumentComparerProvider ArgumentComparerProvider { get; }
        protected IRetryComputePolicy RetryComputePolicy { get; }
        protected IAsyncLockSet<(IFunction, InterceptedInput)>? Locks { get; }                      

        public ComputedInterceptor(
            ConcurrentIdGenerator<int> tagGenerator,
            IComputedRegistry<(IFunction, InterceptedInput)> computedRegistry,
            IArgumentComparerProvider? argumentComparerProvider = null,
            IRetryComputePolicy? retryComputePolicy = null,
            IAsyncLockSet<(IFunction, InterceptedInput)>? locks = null) 
        {
            locks ??= new AsyncLockSet<(IFunction, InterceptedInput)>(ReentryMode.CheckedFail);
            retryComputePolicy ??= Purifier.RetryComputePolicy.Default;
            argumentComparerProvider ??= Autofac.ArgumentComparerProvider.Default;

            _createHandler = CreateHandler;
            _createInterceptedMethod = CreateInterceptedMethod;
            _createTypedHandlerMethod = GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(m => m.Name == nameof(CreateTypedHandler));
            
            TagGenerator = tagGenerator;
            ComputedRegistry = computedRegistry;
            ArgumentComparerProvider = argumentComparerProvider;
            RetryComputePolicy = retryComputePolicy;
            Locks = locks;
        }

        public void Intercept(IInvocation invocation)
        {
            var handler = _handlerCache.GetOrAddChecked(invocation.Method, _createHandler, invocation);
            if (handler == null)
                invocation.Proceed();
            else 
                handler.Invoke(invocation);
        }

        private Action<IInvocation>? CreateHandler(MethodInfo key, IInvocation initialInvocation)
        {
            var methodInfo = initialInvocation.GetConcreteMethodInvocationTarget();
            var method = _interceptedMethodCache.GetOrAddChecked(methodInfo, _createInterceptedMethod);
            if (method == null)
                return null;

            return (Action<IInvocation>) _createTypedHandlerMethod
                .MakeGenericMethod(method.OutputType)
                .Invoke(this, new [] {(object) initialInvocation, method})!;
        }

        protected virtual Action<IInvocation> CreateTypedHandler<TOut>(
            IInvocation initialInvocation, InterceptedMethod method)
        {
            var function = new InterceptedFunction<TOut>(method, 
                TagGenerator, ComputedRegistry, RetryComputePolicy, Locks);
            return invocation => {
                // ReSharper disable once VariableHidesOuterVariable
                var method = function.Method;
                var input = new InterceptedInput(method, invocation);

                // Invoking the function
                var cancellationToken = input.CancellationToken;
                var usedBy = Computed.GetCurrent();

                // Unusual return type
                if (method.ReturnsComputed) {
                    var computedTask = function.InvokeAsync(input, usedBy, null, cancellationToken);
                    // Technically, invocation.ReturnValue could be
                    // already set here - e.g. when it was a cache miss,
                    // and real invocation's async flow (which could complete
                    // synchronously) had already completed.
                    // But we can't return it, because valueTask's async flow
                    // might be still ongoing. So no matter what, we have to
                    // replace the return value with valueTask.Result. 
                    if (method.ReturnsValueTask)
                        // ReSharper disable once HeapView.BoxingAllocation
                        invocation.ReturnValue = new ValueTask<IComputed<TOut>>(computedTask!);
                    else                                                        
                        invocation.ReturnValue = computedTask;
                    return;
                }

                // InvokeAndStripAsync allows to get rid of one extra allocation
                // of a task stripping the result of regular InvokeAsync.
                var task = function.InvokeAndStripAsync(input, usedBy, null, cancellationToken);
                if (method.ReturnsValueTask)
                    // ReSharper disable once HeapView.BoxingAllocation
                    invocation.ReturnValue = new ValueTask<TOut>(task);
                else
                    invocation.ReturnValue = task;
            };
        }

        protected virtual InterceptedMethod? CreateInterceptedMethod(MethodInfo methodInfo)
        {
            var attr = methodInfo.GetCustomAttribute<ComputedAttribute>(true);
            var isEnabled = attr?.IsEnabled ?? true;
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

            var attrKeepAliveTime = attr?.KeepAliveTime ?? Double.MinValue;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            var keepAliveTime = attrKeepAliveTime != double.MinValue
                ? (int?) IntMoment.SecondsToUnits(attrKeepAliveTime)
                : null;

            var invocationTargetType = methodInfo.ReflectedType;
            var parameters = methodInfo.GetParameters();
            var r = new InterceptedMethod {
                MethodInfo = methodInfo,
                OutputType = outputType,
                ReturnsValueTask = returnsValueTask,
                ReturnsComputed = returnsComputed,
                InvocationTargetComparer = ArgumentComparerProvider.GetInvocationTargetComparer(
                    methodInfo, invocationTargetType!),
                ArgumentComparers = new ArgumentComparer[parameters.Length],
                KeepAliveTime = keepAliveTime,  
            };

            for (var i = 0; i < parameters.Length; i++) {
                var p = parameters[i];
                r.ArgumentComparers[i] = ArgumentComparerProvider.GetArgumentComparer(methodInfo, p);
                var parameterType = p.ParameterType;
                if (typeof(CancellationToken).IsAssignableFrom(parameterType))
                    r.CancellationTokenArgumentIndex = i;
            }

            return r;
        }

    }
}
