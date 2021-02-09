using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Internal;

namespace Stl.Fusion.Blazor
{
    public abstract class LiveComponentBase<T> : StatefulComponentBase<ILiveState<T>>
    {
        protected virtual bool SynchronizeComputeStateAsync { get; } = true;

        protected override ILiveState<T> CreateState()
            => SynchronizeComputeStateAsync
                ? StateFactory.NewLive<T>(ConfigureState,
                    async (_, ct) => {
                        // Default CreateState synchronizes ComputeStateAsync call
                        // as per https://github.com/servicetitan/Stl.Fusion/issues/202
                        // You can override it to implement a version w/o sync.
                        var computed = Computed.GetCurrent();
                        var ts = TaskSource.New<T>(false);
                        using var _1 = ExecutionContextEx.SuppressFlow();
                        await Task.Run(() => InvokeAsync(async () => {
                            try {
                                using var _2 = Computed.ChangeCurrent(computed);
                                ts.TrySetResult(await ComputeStateAsync(ct));
                            }
                            catch (OperationCanceledException) {
                                ts.TrySetCanceled();
                            }
                            catch (Exception e) {
                                ts.TrySetException(e);
                            }
                        }), ct);
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
