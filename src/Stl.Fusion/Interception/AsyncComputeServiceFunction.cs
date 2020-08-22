using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stl.Fusion.Interception.Internal;
using Stl.Fusion.Internal;
using Stl.Fusion.Swapping;
using Stl.Generators;

namespace Stl.Fusion.Interception
{
    public class AsyncComputeServiceFunction<T> : ComputeServiceFunctionBase<T>
    {
        public AsyncComputeServiceFunction(
            IServiceProvider serviceProvider,
            InterceptedMethod method,
            Generator<LTag> versionGenerator,
            ILogger<ComputeServiceFunction<T>>? log = null)
            : base(serviceProvider, method, versionGenerator, log)
        {
            if (!method.Options.IsAsyncComputed)
                throw Stl.Internal.Errors.InternalError(
                    $"This type shouldn't be used with {nameof(ComputedOptions)}.{nameof(ComputedOptions.IsAsyncComputed)} == false option.");
        }

        public override async Task<T> InvokeAndStripAsync(
            InterceptedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken = default)
        {
            context ??= ComputeContext.Current;
            ResultBox<T>? output;

            // Read-Lock-RetryRead-Compute-Store pattern

            var computed = TryGetExisting(input);
            if (computed != null) {
                output = await computed.TryUseExistingAsync(context, usedBy, cancellationToken)
                    .ConfigureAwait(false);
                if (output != null)
                    return output.Value;
            }

            using var @lock = await Locks.LockAsync(input, cancellationToken).ConfigureAwait(false);

            computed = TryGetExisting(input);
            if (computed != null) {
                output = await computed.TryUseExistingAsync(context, usedBy, cancellationToken)
                    .ConfigureAwait(false);
                if (output != null)
                    return output.Value;
            }

            computed = (IAsyncComputed<T>) await ComputeAsync(input, computed, cancellationToken)
                .ConfigureAwait(false);
            output = computed.Output; // RenewTimeouts isn't called yet, so it's ok
            computed.UseNew(context, usedBy);
            return output!.Value;
        }

        protected override IComputed<T> CreateComputed(InterceptedInput input, LTag tag)
            => new SwappingComputed<T>(Options, input, tag);

        new protected IAsyncComputed<T>? TryGetExisting(InterceptedInput input)
        {
            var computed = ComputedRegistry.Instance.TryGet(input);
            return computed as IAsyncComputed<T>;
        }
    }
}
