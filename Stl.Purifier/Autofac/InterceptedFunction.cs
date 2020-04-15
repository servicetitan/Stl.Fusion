using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Concurrency;
using Stl.Locking;
using Stl.Purifier.Autofac.Internal;

namespace Stl.Purifier.Autofac
{
    public class InterceptedFunction<TOut> : FunctionBase<InterceptedInput, TOut>
    {
        public InterceptedMethodInfo Method { get; }
        protected ConcurrentIdGenerator<long> TagGenerator { get; }

        public InterceptedFunction(
            InterceptedMethodInfo method,
            ConcurrentIdGenerator<long> tagGenerator,
            IComputedRegistry<(IFunction, InterceptedInput)> computedRegistry,
            IAsyncLockSet<(IFunction, InterceptedInput)>? locks = null) 
            : base(computedRegistry, locks)
        {
            Method = method;
            TagGenerator = tagGenerator;
        }

        public override string ToString() => $"{GetType().Name}({Method})";

        protected override async ValueTask<IComputed<InterceptedInput, TOut>> ComputeAsync(InterceptedInput input, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var workerId = HashCode.Combine(this, input);
            var tag = TagGenerator.Next(workerId);
            var output = new Computed<InterceptedInput, TOut>(this, input, tag);
            try {
                using (Computed.ChangeCurrent(output)) {
                    var method = Method;
                    var proceedInfo = input.ProceedInfo;
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
                // This exception "propagates" as-is
                throw;
            }
            catch (OperationCanceledException) {
                // This exception "propagates" as-is
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
