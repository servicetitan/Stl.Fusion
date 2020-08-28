using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Frozen;
using Stl.Internal;

namespace Stl.Fusion
{
    public interface IMutableState : IState, IMutableResult
    {
        public new interface IOptions : IState.IOptions { }
    }
    public interface IMutableState<T> : IState<T>, IMutableResult<T>, IMutableState { }

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
            set => Update(Result.Value(value));
        }
        public new Exception? Error {
            get => base.Error;
            set => Update(Result.Error<T>(value));
        }
        object? IMutableResult.UntypedValue {
            get => Value;
            set => Update(Result.Value((T) value!));
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

        void IMutableResult.Update(IResult result)
            => Update(result.AsResult<T>());
        public void Update(Result<T> result)
        {
            if (result.IsValue(out var v) && v is IFrozen f)
                f.Freeze();
            lock (Lock) {
                _output = result;
                Computed.Invalidate();
                var task = Computed.UpdateAsync(false, CancellationToken.None);
                if (!task.IsCompleted)
                    throw Errors.InternalError(
                        $"{nameof(IComputed.UpdateAsync)} must complete synchronously here.");
            }
        }

        protected override Task<T> ComputeValueAsync(CancellationToken cancellationToken)
            => _output.AsTask();
    }
}
