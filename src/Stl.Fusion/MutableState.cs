using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Frozen;

namespace Stl.Fusion
{
    public interface IMutableState : IState, IMutableResult
    {
        public new interface IOptions : IState.IOptions { }
        public ValueTask SetAsync(IResult result, CancellationToken cancellationToken);
    }
    public interface IMutableState<T> : IState<T>, IMutableResult<T>, IMutableState
    {
        public ValueTask SetAsync(Result<T> result, CancellationToken cancellationToken);
    }

    public class MutableState<T> : State<T>, IMutableState<T>
    {
        public new class Options : State<T>.Options, IMutableState.IOptions
        {
            public Options()
            {
                ComputedOptions = ComputedOptions.NoAutoInvalidateOnError;
                InitialIsConsistent = true;
            }
        }

        private Result<T> _output;

        public new T Value {
            get => base.Value;
            set => Set(Result.Value(value));
        }
        public new Exception? Error {
            get => base.Error;
            set => Set(Result.Error<T>(value));
        }
        object? IMutableResult.UntypedValue {
            get => Value;
            set => Set(Result.Value((T) value!));
        }

        public MutableState(
            Options options,
            IServiceProvider serviceProvider,
            Option<Result<T>> initialOutput = default,
            object? argument = null,
            bool initialize = true)
            : base(options, serviceProvider, argument, false)
        {
            _output = initialOutput.IsSome(out var o) ? o : options.InitialOutputFactory.Invoke(this);
            // ReSharper disable once VirtualMemberCallInConstructor
            if (initialize) Initialize(options);
        }

        protected override void Initialize(State<T>.Options options)
        {
            var computed = CreateComputed();
            computed.TrySetOutput(_output);
            Computed = computed;
        }

        void IMutableResult.Set(IResult result)
            => Set(result.AsResult<T>());
        public void Set(Result<T> result)
        {
            if (result.IsValue(out var v) && v is IFrozen f)
                f.Freeze();
            IStateSnapshot<T> snapshot;
            lock (Lock) {
                snapshot = Snapshot;
                _output = result;
            }
            snapshot.Computed.Invalidate();
        }

        public async ValueTask SetAsync(IResult result, CancellationToken cancellationToken)
            => await SetAsync(result.AsResult<T>(), cancellationToken).ConfigureAwait(false);
        public async ValueTask SetAsync(Result<T> result, CancellationToken cancellationToken)
        {
            Set(result);
            await Computed.UpdateAsync(false, cancellationToken).ConfigureAwait(false);
        }

        protected internal override void OnInvalidated(IComputed<T> computed)
        {
            base.OnInvalidated(computed);
            if (Snapshot.Computed == computed)
                computed.UpdateAsync(false);
        }

        protected override Task<T> ComputeValueAsync(CancellationToken cancellationToken)
            => _output.AsTask();
    }
}
