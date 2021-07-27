using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;
using Stl.Locking;

namespace Stl.Fusion.Swapping
{
    public class SwappingComputed<T> : Computed<T>, IAsyncComputed<T>, ISwappable
    {
        private readonly Lazy<AsyncLock> _swapOutputLockLazy =
            new(() => new AsyncLock(ReentryMode.UncheckedDeadlock));
        private volatile ResultBox<T>? _maybeOutput;

        protected AsyncLock SwapOutputLock => _swapOutputLockLazy.Value;
        public override Result<T> Output {
            get {
                this.AssertConsistencyStateIsNot(ConsistencyState.Computing);
                var output = _maybeOutput;
                if (output == null)
                    throw Errors.OutputIsUnloaded();
                return output.AsResult();
            }
        }
        IResult? IAsyncComputed.MaybeOutput => MaybeOutput;
        public ResultBox<T>? MaybeOutput => _maybeOutput;

        public SwappingComputed(ComputedOptions options, ComputeMethodInput input, LTag version)
            : base(options, input, version) { }
        protected SwappingComputed(ComputedOptions options, ComputeMethodInput input, ResultBox<T> maybeOutput, LTag version, bool isConsistent)
            : base(options, input, default, version, isConsistent)
            => _maybeOutput = maybeOutput;

        public override bool TrySetOutput(Result<T> output)
        {
            if (Options.RewriteErrors && !output.IsValue(out _, out var error)) {
                var errorRewriter = Function.Services.GetRequiredService<IErrorRewriter>();
                output = Result.Error<T>(errorRewriter.Rewrite(this, error));
            }
            if (ConsistencyState != ConsistencyState.Computing)
                return false;
            lock (Lock) {
                if (ConsistencyState != ConsistencyState.Computing)
                    return false;
                SetStateUnsafe(ConsistencyState.Consistent);
                Interlocked.Exchange(ref _maybeOutput, output);
            }
            OnOutputSet(output.AsResult());
            return true;
        }

        async ValueTask<IResult?> IAsyncComputed.GetOutput(CancellationToken cancellationToken)
            => await GetOutput(cancellationToken).ConfigureAwait(false);
        public async ValueTask<ResultBox<T>?> GetOutput(CancellationToken cancellationToken)
        {
            var maybeOutput = MaybeOutput;
            if (maybeOutput != null)
                return maybeOutput;

            // Double-check locking
            using var _ = await SwapOutputLock.Lock(cancellationToken).ConfigureAwait(false);

            maybeOutput = MaybeOutput;
            if (maybeOutput != null)
                return maybeOutput;

            var swapService = Function.Services.GetService<ISwapService>() ?? NoSwapService.Instance;
            maybeOutput = await swapService.Load((Input, Version), cancellationToken) as ResultBox<T>;
            if (maybeOutput == null) {
                Invalidate();
                return null;
            }
            Interlocked.Exchange(ref _maybeOutput, maybeOutput);
            return maybeOutput;
        }

        public override async ValueTask<T> Use(CancellationToken cancellationToken = default)
        {
            var usedBy = Computed.GetCurrent();
            var context = ComputeContext.Current;
            if ((context.CallOptions & CallOptions.TryGetExisting) != 0) // Both TryGetExisting & Invalidate
                throw Errors.InvalidContextCallOptions(context.CallOptions);
            if (IsConsistent()) {
                var resultBox = await this.TryUseExistingFromUse(context, usedBy, cancellationToken)
                    .ConfigureAwait(false);
                if (resultBox != null)
                    return resultBox.Value;
            }
            var computed = await Function
                .Invoke(Input, usedBy, context, cancellationToken)
                .ConfigureAwait(false);
            return computed.Value;
        }

        public async ValueTask Swap(CancellationToken cancellationToken = default)
        {
            this.AssertConsistencyStateIsNot(ConsistencyState.Computing);
            using var _ = await SwapOutputLock.Lock(cancellationToken).ConfigureAwait(false);
            if (MaybeOutput == null)
                return;
            var swapService = Function.Services.GetService<ISwapService>() ?? NoSwapService.Instance;
            await swapService.Store((Input, Version), MaybeOutput, cancellationToken);
            Interlocked.Exchange(ref _maybeOutput, null);
            RenewTimeouts();
        }

        public override void RenewTimeouts()
        {
            if (ConsistencyState == ConsistencyState.Invalidated)
                return;
            if (MaybeOutput != null) {
                var swappingOptions = Options.SwappingOptions;
                if (swappingOptions.IsEnabled && swappingOptions.SwapTime > TimeSpan.Zero) {
                    Timeouts.Swap.AddOrUpdateToLater(this, Timeouts.Clock.Now + swappingOptions.SwapTime);
                    return;
                }
            }
            base.RenewTimeouts();
        }

        public override void CancelTimeouts()
        {
            var swappingOptions = Options.SwappingOptions;
            if (swappingOptions.IsEnabled && swappingOptions.SwapTime > TimeSpan.Zero)
                Timeouts.Swap.Remove(this);
            base.CancelTimeouts();
        }
    }
}
