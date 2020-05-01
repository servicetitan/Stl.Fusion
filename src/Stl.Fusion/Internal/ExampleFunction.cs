using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl.Concurrency;
using Stl.Locking;

namespace Stl.Fusion.Internal
{
    // Unused, left here solely as an example of simpler impl. 
    internal class ExampleFunction<TIn, TOut> : FunctionBase<TIn, TOut>
        where TIn : notnull
    {
        protected Func<TIn, CancellationToken, ValueTask<TOut>> Implementation { get; }
        protected ConcurrentIdGenerator<int> TagGenerator { get; }

        public ExampleFunction(
            Func<TIn, CancellationToken, ValueTask<TOut>> implementation,
            ConcurrentIdGenerator<int> tagGenerator,
            IComputedRegistry<(IFunction, TIn)> computedRegistry,
            IRetryComputePolicy? retryComputePolicy = null,
            IAsyncLockSet<(IFunction, TIn)>? locks = null) 
            : base(computedRegistry, retryComputePolicy, locks)
        {
            Implementation = implementation;
            TagGenerator = tagGenerator;
        }

        public override string ToString() => $"{GetType().Name}({Implementation})";

        protected override async ValueTask<IComputed<TOut>> ComputeAsync(TIn input, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var workerId = HashCode.Combine(this, input);
            var tag = TagGenerator.Next(workerId);
            var computed = new Computed<TIn, TOut>(this, input, tag);
            try {
                using (Computed.ChangeCurrent(computed)) {
                    var value = await Implementation.Invoke(input, cancellationToken).ConfigureAwait(false);
                    computed.TrySetOutput(value!);
                }
            }
            catch (OperationCanceledException) {
                // This exception "propagates" as-is
                throw;
            }
            catch (Exception e) { 
                computed.TrySetOutput(Result.Error<TOut>(e));
                // Weird case: if the output is already set, all we can
                // is to ignore the exception we've just caught;
                // throwing it further will probably make it just worse,
                // since the the caller have to take this scenario into acc.
            }
            return computed;
        }
    }

    internal static class ExampleFunction
    {
        public static ExampleFunction<Unit, TOut> New<TOut>(
            Func<CancellationToken, ValueTask<TOut>> implementation,
            IServiceProvider services)
        {
            return new ExampleFunction<Unit, TOut>((u, ct) => implementation(ct),
                services.GetRequiredService<ConcurrentIdGenerator<int>>(),
                services.GetRequiredService<IComputedRegistry<(IFunction, Unit)>>(),
                services.GetService<IRetryComputePolicy>(),
                services.GetService<IAsyncLockSet<(IFunction, Unit)>>()
            );
        }

        public static ExampleFunction<TIn, TOut> New<TIn, TOut>(
            Func<TIn, CancellationToken, ValueTask<TOut>> implementation,
            IServiceProvider services)
            where TIn : notnull
        {
            return new ExampleFunction<TIn, TOut>(implementation,
                services.GetRequiredService<ConcurrentIdGenerator<int>>(),
                services.GetRequiredService<IComputedRegistry<(IFunction, TIn)>>(),
                services.GetService<IRetryComputePolicy>(),
                services.GetService<IAsyncLockSet<(IFunction, TIn)>>()
            );
        }
    }
}
