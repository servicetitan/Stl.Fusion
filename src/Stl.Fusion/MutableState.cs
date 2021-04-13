using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.Internal;
using Errors = Stl.Internal.Errors;

namespace Stl.Fusion
{
    public interface IMutableState : IState, IMutableResult
    {
        public new interface IOptions : IState.IOptions { }
    }

    public interface IMutableState<T> : IState<T>, IMutableResult<T>, IMutableState
    { }

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
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            get => Value;
            set => Set(Result.Value((T) value!));
        }

        // This constructor is used by generic service descriptor for IMutableState<T>
        public MutableState(IServiceProvider services)
            : this(new Options(), services) { }
        // This constructor is used by StateFactory
        public MutableState(
            Options options,
            IServiceProvider services,
            Option<Result<T>> initialOutput = default,
            bool initialize = true)
            : base(options, services, false)
        {
            _output = initialOutput.IsSome(out var o) ? o : options.InitialOutputFactory.Invoke(this);
            // ReSharper disable once VirtualMemberCallInConstructor
            if (initialize) Initialize(options);
        }

        protected override void Initialize(State<T>.Options options)
            => CreateComputed();

        void IMutableResult.Set(IResult result)
            => Set(result.Cast<T>());
        public void Set(Result<T> result)
        {
            lock (Lock) {
                var snapshot = Snapshot;
                _output = result;
                // Better to do this inside the lock, since it will be
                // re-acquired later - see InvokeAsync and InvokeAndStrip overloads
                snapshot.Computed.Invalidate();
            }
        }

        // Protected methods

        protected internal override void OnInvalidated(IComputed<T> computed)
        {
            base.OnInvalidated(computed);
            if (Snapshot.Computed == computed)
                computed.Update();
        }

        protected override Task<IComputed<T>> Invoke(
            State<T> input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
        {
            // This method should always complete synchronously in IMutableState<T>
            if (input != this)
                // This "Function" supports just a single input == this
                throw new ArgumentOutOfRangeException(nameof(input));

            context ??= ComputeContext.Current;

            var result = Computed;
            if (result.TryUseExisting(context, usedBy))
                return Task.FromResult(result);

            // Double-check locking
            lock (Lock) {
                result = Computed;
                if (result.TryUseExisting(context, usedBy))
                    return Task.FromResult(result);

                OnUpdating(result);
                result = CreateComputed();
                result.UseNew(context, usedBy);
                context.TryCapture(result);
                return Task.FromResult(result);
            }
        }

        protected override Task<T> InvokeAndStrip(
            State<T> input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
        {
            // This method should always complete synchronously in IMutableState<T>
            if (input != this)
                // This "Function" supports just a single input == this
                throw new ArgumentOutOfRangeException(nameof(input));

            context ??= ComputeContext.Current;

            var result = Computed;
            if (result.TryUseExisting(context, usedBy))
                return result.StripToTask(context);

            // Double-check locking
            lock (Lock) {
                result = Computed;
                if (result.TryUseExisting(context, usedBy))
                    return result.StripToTask(context);

                OnUpdating(result);
                result = CreateComputed();
                result.UseNew(context, usedBy);
                context.TryCapture(result);
                return result.StripToTask(context);
            }
        }

        protected override StateBoundComputed<T> CreateComputed()
        {
            var computed = base.CreateComputed();
            computed.SetOutput(_output);
            Computed = computed;
            return computed;
        }

        protected override Task<T> Compute(CancellationToken cancellationToken)
            => throw Errors.InternalError("This method should never be called.");
    }
}
