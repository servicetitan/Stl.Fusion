using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl.Frozen;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;
using Stl.Locking;

namespace Stl.Fusion.Swapping
{
    public class SwappingComputed<T> : Computed<T>, IAsyncComputed<T>, ISwappable
    {
        private readonly Lazy<AsyncLock> _swapOutputLockLazy =
            new Lazy<AsyncLock>(() => new AsyncLock(ReentryMode.UncheckedDeadlock));
        private volatile ResultBox<T>? _maybeOutput;

        protected AsyncLock SwapOutputLock => _swapOutputLockLazy.Value;
        public override Result<T> Output {
            get {
                AssertStateIsNot(ComputedState.Computing);
                var output = _maybeOutput;
                if (output == null)
                    throw Errors.OutputIsUnloaded();
                return output.AsResult();
            }
        }
        IResult? IAsyncComputed.MaybeOutput => MaybeOutput;
        public ResultBox<T>? MaybeOutput => _maybeOutput;

        public SwappingComputed(ComputedOptions options, InterceptedInput input, LTag version)
            : base(options, input, version) { }
        protected SwappingComputed(ComputedOptions options, InterceptedInput input, ResultBox<T> maybeOutput, LTag version, bool isConsistent)
            : base(options, input, default, version, isConsistent)
            => _maybeOutput = maybeOutput;

        public override bool TrySetOutput(Result<T> output)
            => TrySetOutput(new ResultBox<T>(output), false);
        public bool TrySetOutput(ResultBox<T> output, bool isFromCache)
        {
            if (output.IsValue(out var v) && v is IFrozen f)
                f.Freeze();
            if (State != ComputedState.Computing)
                return false;
            lock (Lock) {
                if (State != ComputedState.Computing)
                    return false;
                SetStateUnsafe(ComputedState.Consistent);
                Interlocked.Exchange(ref _maybeOutput, output);
            }
            OnOutputSet(output.AsResult());
            return true;
        }

        async ValueTask<IResult?> IAsyncComputed.GetOutputAsync(CancellationToken cancellationToken)
            => await GetOutputAsync(cancellationToken).ConfigureAwait(false);
        public async ValueTask<ResultBox<T>?> GetOutputAsync(CancellationToken cancellationToken)
        {
            var maybeOutput = MaybeOutput;
            if (maybeOutput != null)
                return maybeOutput;

            // Double-check locking
            using var _ = await SwapOutputLock.LockAsync(cancellationToken).ConfigureAwait(false);

            maybeOutput = MaybeOutput;
            if (maybeOutput != null)
                return maybeOutput;

            var swapService = Function.ServiceProvider.GetRequiredService<ISwapService>();
            maybeOutput = await swapService.LoadAsync((Input, Version), cancellationToken) as ResultBox<T>;
            if (maybeOutput == null) {
                Invalidate();
                return null;
            }
            Interlocked.Exchange(ref _maybeOutput, maybeOutput);
            return maybeOutput;
        }

        public async ValueTask SwapAsync(CancellationToken cancellationToken = default)
        {
            AssertStateIsNot(ComputedState.Computing);
            using var _ = await SwapOutputLock.LockAsync(cancellationToken).ConfigureAwait(false);
            if (MaybeOutput == null)
                return;
            var swapService = Function.ServiceProvider.GetRequiredService<ISwapService>();
            await swapService.StoreAsync((Input, Version), MaybeOutput, cancellationToken);
            Interlocked.Exchange(ref _maybeOutput, null);
            RenewTimeouts();
        }

        public override void RenewTimeouts()
        {
            if (State == ComputedState.Invalidated)
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
