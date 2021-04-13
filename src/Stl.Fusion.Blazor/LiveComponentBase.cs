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
        RecomputeOnParametersSet = 0x2,
    }

    public abstract class LiveComponentBase<T> : StatefulComponentBase<ILiveState<T>>
    {
        protected LiveComponentOptions Options { get; set; } =
            LiveComponentOptions.SynchronizeComputeState
            | LiveComponentOptions.RecomputeOnParametersSet;

        // Typically State depends on component parameters, so...
        protected override void OnParametersSet()
        {
            if (0 != (Options & LiveComponentOptions.RecomputeOnParametersSet))
                State.Recompute();
        }

        protected override ILiveState<T> CreateState()
            => 0 != (Options & LiveComponentOptions.SynchronizeComputeState)
                ? StateFactory.NewLive<T>(ConfigureState,
                    async (_, ct) => {
                        // Synchronizes ComputeState call as per:
                        // https://github.com/servicetitan/Stl.Fusion/issues/202
                        var ts = TaskSource.New<T>(false);
                        await InvokeAsync(async () => {
                            try {
                                ts.TrySetResult(await ComputeState(ct));
                            }
                            catch (OperationCanceledException) {
                                ts.TrySetCanceled();
                            }
                            catch (Exception e) {
                                ts.TrySetException(e);
                            }
                        });
                        return await ts.Task.ConfigureAwait(false);
                    })
                : StateFactory.NewLive<T>(ConfigureState,
                    (_, ct) => ComputeState(ct));

        protected virtual void ConfigureState(LiveState<T>.Options options) { }
        protected abstract Task<T> ComputeState(CancellationToken cancellationToken);
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
            Locals.Updated += (_, _) => State.Recompute();
            base.OnInitialized();
        }

        protected virtual IMutableState<TLocals> CreateLocals()
            => StateFactory.NewMutable(ConfigureLocals, Option<Result<TLocals>>.None);

        protected virtual void ConfigureLocals(MutableState<TLocals>.Options options) { }
    }
}
