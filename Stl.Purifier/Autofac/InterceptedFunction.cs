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
        public InterceptedMethod Method { get; }
        protected ConcurrentIdGenerator<long> TagGenerator { get; }

        public InterceptedFunction(
            InterceptedMethod method,
            ConcurrentIdGenerator<long> tagGenerator,
            IComputedRegistry<(IFunction, InterceptedInput)> computedRegistry,
            IAsyncLockSet<(IFunction, InterceptedInput)>? locks = null) 
            : base(computedRegistry, locks)
        {
            Method = method;
            TagGenerator = tagGenerator;
        }

        public override string ToString()
        {
            var mi = Method.MethodInfo;
            return $"{GetType().Name}({mi.DeclaringType!.Name}.{mi.Name})";
        }

        protected override async ValueTask<IComputed<TOut>> ComputeAsync(InterceptedInput input, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var tag = TagGenerator.Next(input.HashCode);
            var output = new Computed<InterceptedInput, TOut>(this, input, tag);
            try {
                using var _ = Computed.ChangeCurrent(output);
                var returnValue = input.InvokeOriginalFunction(output, cancellationToken);
                var method = Method;
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

        public override IComputed<TOut>? TryGetCached(InterceptedInput input, IComputed? usedBy = null)
        {
            var output = base.TryGetCached(input, usedBy);
            if (output != null && (input.CallOptions.Action & CallAction.CaptureComputed) != 0)
                ComputedCapture.TryCapture(output);
            return output;
        }
    }
}
