using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Concurrency;
using Stl.Locking;

namespace Stl.Purifier
{
    public class Function<TIn, TOut> : FunctionBase<TIn, TOut>
        where TIn : notnull
    {
        protected Func<TIn, CancellationToken, ValueTask<TOut>> Implementation { get; }
        protected ConcurrentIdGenerator<long> TagGenerator { get; }

        public Function(
            Func<TIn, CancellationToken, ValueTask<TOut>> implementation,
            ConcurrentIdGenerator<long> tagGenerator,
            IComputedRegistry<(IFunction, TIn)> computedRegistry,
            IAsyncLockSet<(IFunction, TIn)>? locks = null) 
            : base(computedRegistry, locks)
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
            catch (TaskCanceledException) {
                // This exception "propagates" as-is
                throw;
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
}
