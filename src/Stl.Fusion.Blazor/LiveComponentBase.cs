using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Internal;

namespace Stl.Fusion.Blazor
{
    [Flags]
    public enum LiveComponentOptions
    {
        SynchronizeComputeState = 0x1,
        InvalidateOnParametersSet = 0x2,
    }

    public abstract class LiveComponentBase<T> : StatefulComponentBase<ILiveState<T>>
    {
        protected LiveComponentOptions Options { get; set; } =
            LiveComponentOptions.SynchronizeComputeState
            | LiveComponentOptions.InvalidateOnParametersSet;

        // Typically State depends on component parameters, so...
        protected override void OnParametersSet()
        {
            if (0 != (Options & LiveComponentOptions.InvalidateOnParametersSet))
                InvalidateState();
        }

        /// <summary>
        /// Typically you need to call this method after UI actions to ensure
        /// the update from server is requested instantly.
        /// </summary>
        /// <param name="cancelUpdateDelay">Cancels update delay, i.e. requests instant update.</param>
        /// <param name="cancellationDelay">The delay between this call and update delay cancellation.
        /// The default (null) means it's governed by <see cref="IUpdateDelayer{T}"/>, which does this
        /// in 50ms by default.</param>
        protected void InvalidateState(bool cancelUpdateDelay = true, TimeSpan? cancellationDelay = null)
        {
            State.Invalidate();
            if (cancelUpdateDelay)
                State.UpdateDelayer.CancelDelays(cancellationDelay);
        }

        protected override ILiveState<T> CreateState()
            => 0 != (Options & LiveComponentOptions.SynchronizeComputeState)
                ? StateFactory.NewLive<T>(ConfigureState,
                    async (_, ct) => {
                        // Synchronizes ComputeStateAsync call as per:
                        // https://github.com/servicetitan/Stl.Fusion/issues/202
                        var ts = TaskSource.New<T>(false);
                        await InvokeAsync(async () => {
                            try {
                                ts.TrySetResult(await ComputeStateAsync(ct));
                            }
                            catch (OperationCanceledException) {
                                ts.TrySetCanceled();
                            }
                            catch (Exception e) {
                                ts.TrySetException(e);
                            }
                        });
                        return await ts.Task.ConfigureAwait(false);
                    }, this)
                : StateFactory.NewLive<T>(ConfigureState,
                    (_, ct) => ComputeStateAsync(ct), this);

        protected virtual void ConfigureState(LiveState<T>.Options options) { }
        protected abstract Task<T> ComputeStateAsync(CancellationToken cancellationToken);
    }

    public abstract class LiveComponentBase<T, TLocals> : LiveComponentBase<T>
    {
        private IMutableState<TLocals>? _locals;

        protected IMutableState<TLocals> Locals {
            get => _locals!;
            set {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (_locals == value)
                    return;
                if (_locals != null)
                    throw Errors.AlreadyInitialized(nameof(State));
                _locals = value;
            }
        }

        protected override void OnInitialized()
        {
            // ReSharper disable once ConstantNullCoalescingCondition
            Locals ??= CreateLocals();
            Locals.Updated += (s, e) => {
                State.Invalidate();
                State.CancelUpdateDelay();
            };
            base.OnInitialized();
        }

        protected virtual IMutableState<TLocals> CreateLocals()
            => StateFactory.NewMutable(ConfigureLocals, Option<Result<TLocals>>.None);

        protected virtual void ConfigureLocals(MutableState<TLocals>.Options options) { }
    }
}
