using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Concurrency;

namespace Stl.Fusion.Autofac
{
    public class InterceptedFunction<TOut> : FunctionBase<InterceptedInput, TOut>
    {
        public InterceptedMethod Method { get; }
        protected ConcurrentIdGenerator<LTag> LTagGenerator { get; }

        public InterceptedFunction(
            InterceptedMethod method,
            ConcurrentIdGenerator<LTag> lTagGenerator,
            IComputedRegistry computedRegistry,
            IComputeRetryPolicy? retryPolicy = null) 
            : base(computedRegistry, retryPolicy)
        {
            Method = method;
            LTagGenerator = lTagGenerator;
        }

        public override string ToString()
        {
            var mi = Method.MethodInfo;
            return $"Intercepted:{mi.DeclaringType!.Name}.{mi.Name}";
        }

        protected override async ValueTask<IComputed<TOut>> ComputeAsync(
            InterceptedInput input, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var tag = LTagGenerator.Next(input.HashCode);
            var output = new Computed<InterceptedInput, TOut>(input, tag);
            var method = Method;
            var keepAliveTime = method.KeepAliveTime;
            if (keepAliveTime.HasValue)
                output.KeepAliveTime = keepAliveTime.GetValueOrDefault();
            try {
                using var _ = Computed.ChangeCurrent(output);
                var resultTask = input.InvokeOriginalFunction(output, cancellationToken);
                if (method.ReturnsComputed) {
                    if (method.ReturnsValueTask) {
                        var task = (ValueTask<IComputed<TOut>>) resultTask;
                        await task.ConfigureAwait(false);
                        // output == task.Result here, so no need to call output.TrySetOutput(...)
                    }
                    else {
                        var task = (Task<IComputed<TOut>>) resultTask;
                        await task.ConfigureAwait(false);
                        // output == task.Result here, so no need to call output.TrySetOutput(...)
                    }
                }
                else {
                    if (method.ReturnsValueTask) {
                        var task = (ValueTask<TOut>) resultTask;
                        var value = await task.ConfigureAwait(false);
                        output.TrySetOutput(value!);
                    }
                    else {
                        var task = (Task<TOut>) resultTask;
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
    }
}
