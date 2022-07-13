using Stl.Fusion.Internal;
using Errors = Stl.Internal.Errors;

namespace Stl.Fusion;

public interface IMutableState : IState, IMutableResult
{
    public new interface IOptions : IState.IOptions { }
}

public interface IMutableState<T> : IState<T>, IMutableResult<T>, IMutableState
{ }

public class MutableState<T> : State<T>, IMutableState<T>
{
    public new record Options : State<T>.Options, IMutableState.IOptions
    {
        public Options()
            => ComputedOptions = ComputedOptions.MutableStateDefault;
    }

    private Result<T> _output;

    public new T Value {
        get => base.Value;
        set => Set(Result.Value(value));
    }
    public new Exception? Error {
        get => base.Error;
        set => Set(Result.Error<T>(value!));
    }
    object? IMutableResult.UntypedValue {
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        get => Value;
        set => Set(Result.Value((T) value!));
    }

    public MutableState(IServiceProvider services)
        : this(new(), services) { }
    public MutableState(
        Options options,
        IServiceProvider services,
        bool initialize = true)
        : base(options, services, false)
    {
        _output = options.InitialOutput;
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
            if (_output == result)
                return;
            var snapshot = Snapshot;
            _output = result;
            // We do this inside the lock by a few reasons:
            // 1. Otherwise the lock will be acquired twice -
            //    see OnInvalidated & Invoke overloads below.
            // 2. It's quite convenient if Set, while being
            //    non-async, synchronously updates the mutable
            //    state.
            // 3. If all the updates are synchronous, we don't
            //    need async lock that's used by regular
            //    IComputed instances.
            snapshot.Computed.Invalidate();
        }
    }

    // Protected methods

    protected internal override void OnInvalidated(IComputed<T> computed)
    {
        base.OnInvalidated(computed);
        if (Snapshot.Computed != computed)
            return;
        var updateTask = computed.Update();
        if (!updateTask.IsCompleted)
            throw Errors.InternalError("Update() task must complete synchronously here.");
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
