using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Internal;

namespace Stl.Fusion.Blazor
{
    public abstract class LiveComponentBase<T> : StatefulComponentBase<ILiveState<T>>
    {
        protected override void OnInitialized()
        {
            State ??= StateFactory.NewLive<T>(ConfigureState, (_, ct) => ComputeStateAsync(ct), this);
            base.OnInitialized();
        }

        protected virtual void ConfigureState(LiveState<T>.Options options) { }

        protected virtual Task<T> ComputeStateAsync(CancellationToken cancellationToken)
            // Return the previous value by default, i.e. doesn't change anything
            => State.AsTask();
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
            Locals ??= StateFactory.NewMutable(ConfigureLocals, Option<Result<TLocals>>.None);
            State ??= StateFactory.NewLive<T>(ConfigureState, (_, ct) => ComputeStateAsync(ct), this);
            Locals.Updated += (s, e) => State.CancelUpdateDelay();
            base.OnInitialized();
        }

        protected virtual void ConfigureLocals(MutableState<TLocals>.Options options) { }
    }
}
