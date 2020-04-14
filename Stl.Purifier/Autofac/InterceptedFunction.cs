using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Stl.Concurrency;
using Stl.Locking;

namespace Stl.Purifier.Autofac
{
    public class InterceptedFunction<TOut> : FunctionBase<ArrayKey, TOut>
    {
        public ExtendedMethodInfo Method { get; }
        protected ConcurrentIdGenerator<long> TagGenerator { get; }

        public InterceptedFunction(
            ExtendedMethodInfo method,
            ConcurrentIdGenerator<long> tagGenerator,
            IComputedRegistry<(IFunction, ArrayKey)> computedRegistry,
            IAsyncLockSet<(IFunction, ArrayKey)>? locks = null) 
            : base(computedRegistry, locks)
        {
            Method = method;
            TagGenerator = tagGenerator;
        }

        protected override async ValueTask<IComputed<ArrayKey, TOut>> ComputeAsync(ArrayKey input, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var workerId = HashCode.Combine(this, input);
            var tag = TagGenerator.Next(workerId);
            var output = new Computed<ArrayKey, TOut>(this, input, tag);
            try {
                using (Computed.ChangeCurrent(output)) {
                    var method = Method;
                    var proceedInfo = (IInvocationProceedInfo) input.Arguments[method.ProceedInfoArgumentIndex];
                    var invocation = proceedInfo.GetInvocation();
                    proceedInfo.Invoke();
                    var returnValue = invocation.ReturnValue;
                    if (method.ReturnsValueTask) {
                        if (method.ReturnsComputed) {
                            var task = (ValueTask<IComputed<TOut>>) returnValue;
                            await task.ConfigureAwait(false);
                            // No need to set output, b/c it is already set
                        }
                        else {
                            var task = (ValueTask<TOut>) returnValue;
                            var value = await task.ConfigureAwait(false);
                            output.TrySetOutput(value!);
                        }
                    }
                    else {
                        if (method.ReturnsComputed) {
                            var task = (Task<IComputed<TOut>>) returnValue;
                            await task.ConfigureAwait(false);
                            // No need to set output, b/c it is already set
                        }
                        else {
                            var task = (Task<TOut>) returnValue;
                            var value = await task.ConfigureAwait(false);
                            output.TrySetOutput(value!);
                        }
                    }
                }
            }
            catch (TaskCanceledException) {
                // That's the only exception that "propagates" as-is
                throw;
            }
            catch (Exception e) { 
                output.TrySetOutput(Result.Error<TOut>(e));
                // Weird case: if the output is already set, all we can
                // is to ignore the exception we've just caught;
                // throwing it further will probably make it just worse,
                // since the the caller have to take this scenario into acc.
            }
            return output;
        }
    }
}
