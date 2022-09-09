using Stl.Fusion.Internal;

namespace Stl.Fusion.Bridge;

public abstract class Replica : ComputedInput, IFunction, IDisposable, IEquatable<Replica>
{
    protected IReplicatorImpl ReplicatorImpl { get; }

    public ComputedOptions ComputedOptions { get; }
    public PublicationRef PublicationRef { get; }
    public IReplicator Replicator => ReplicatorImpl;
    public IServiceProvider Services => ReplicatorImpl.Services;

    public abstract IReplicaComputed UntypedComputed { get; }
    public abstract bool IsUpdateRequested { get; }
    public abstract Exception? UpdateError { get; }

    protected Replica(ComputedOptions computedOptions, PublicationRef publicationRef, IReplicatorImpl replicatorImpl)
    {
        ComputedOptions = computedOptions;
        PublicationRef = publicationRef;
        ReplicatorImpl = replicatorImpl;
        Initialize(this, PublicationRef.GetHashCode());
    }

    public override string ToString()
        => $"{GetType().Name}({PublicationRef})";

    // Dispose

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        try {
            ReplicatorImpl.OnReplicaDisposed(this);
        }
        catch {
            // Intended
        }
        ReplicaRegistry.Instance.Remove(this);
    }

    // Called for temp. replicas that were never exposed by ReplicaRegistry
    public void DisposeTemporaryInstance()
        => GC.SuppressFinalize(this);

    // Abstract members
    public abstract Task RequestUpdate(CancellationToken cancellationToken = default);
    public abstract bool ApplyFailedUpdate(Exception? error, CancellationToken cancelledToken);

#pragma warning disable MA0025
    ValueTask<IComputed> IFunction.Invoke(ComputedInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    Task IFunction.InvokeAndStrip(ComputedInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();
#pragma warning restore MA0025

    // Equality

    public bool Equals(Replica? other)
        => !ReferenceEquals(null, other) && PublicationRef == other.PublicationRef;
    public override bool Equals(ComputedInput? other)
        => other is Replica ri && PublicationRef == ri.PublicationRef;
    public override bool Equals(object? obj)
        => Equals(obj as Replica);
    public override int GetHashCode()
        => HashCode;
}

public sealed class Replica<T> : Replica, IFunction<T>
{
    private volatile ReplicaComputed<T> _computed = null!;
    private volatile Exception? _updateError;
    private volatile Task<Unit>? _updateRequestTask;
    private readonly object _lock = new();

    public ReplicaComputed<T> Computed => _computed;
    public override IReplicaComputed UntypedComputed => _computed;
    public override bool IsUpdateRequested => _updateRequestTask != null;
    public override Exception? UpdateError => _updateError;

    public Replica(
        ComputedOptions computedOptions,
        PublicationStateInfo<T> info,
        IReplicatorImpl replicatorImpl,
        bool isUpdateRequested = false)
        : base(computedOptions, info.PublicationRef, replicatorImpl)
    {
        if (computedOptions.SwappingOptions.IsEnabled)
            throw Errors.UnsupportedComputedOptions(GetType());
        // ReSharper disable once VirtualMemberCallInConstructor
        ApplySuccessfulUpdate(info.Output, info.Version, info.IsConsistent);
        if (isUpdateRequested)
            // ReSharper disable once VirtualMemberCallInConstructor
            _updateRequestTask = CreateUpdateRequestTask();
    }

    // We want to make sure the replicas are connected to
    // publishers only while they're used.
#pragma warning disable MA0055
    ~Replica() => Dispose(false);
#pragma warning restore MA0055

    public override Task RequestUpdate(CancellationToken cancellationToken = default)
    {
        var updateRequestTask = _updateRequestTask;
        if (updateRequestTask != null)
            return updateRequestTask.WaitAsync(cancellationToken);
        // Double check locking
        lock (_lock) {
            updateRequestTask = _updateRequestTask;
            if (updateRequestTask != null)
                return updateRequestTask.WaitAsync(cancellationToken);
            _updateRequestTask = updateRequestTask = CreateUpdateRequestTask();
            ReplicatorImpl.Subscribe(this);
            return updateRequestTask.WaitAsync(cancellationToken);
        }
    }

    public bool ApplySuccessfulUpdate(Result<T>? output, LTag version, bool isConsistent)
    {
        Task<Unit>? updateRequestTask;
        lock (_lock) {
            // 1. Update Computed & UpdateError
            _updateError = null;
            var oldComputed = _computed;

            if (oldComputed == null! || oldComputed.Version != version)
                ReplaceComputedUnsafe(oldComputed, output, version, isConsistent);
            else if (oldComputed.IsConsistent() != isConsistent) {
                if (isConsistent)
                    ReplaceComputedUnsafe(oldComputed, output, version, isConsistent);
                else
                    oldComputed?.Invalidate();
            }

            // 2. Complete UpdateRequestTask
            (updateRequestTask, _updateRequestTask) = (_updateRequestTask, null);
        }

        if (updateRequestTask != null) {
            var updateRequestTaskSource = TaskSource.For(updateRequestTask);
            updateRequestTaskSource.TrySetResult(default);
        }
        return true;
    }

    public override bool ApplyFailedUpdate(Exception? error, CancellationToken cancelledToken)
    {
        ReplicaComputed<T>? computed;
        Task<Unit>? updateRequestTask;
        lock (_lock) {
            // 1. Update Computed & UpdateError
            computed = _computed;
            _updateError = error;

            // 2. Complete UpdateRequestTask
            (updateRequestTask, _updateRequestTask) = (_updateRequestTask, null);
        }

        if (error != null)
            computed.Invalidate();
        if (updateRequestTask != null) {
            var result = new Result<Unit>(default, error);
            var updateRequestTaskSource = TaskSource.For(updateRequestTask);
            updateRequestTaskSource.TrySetFromResult(result, cancelledToken);
        }
        return true;
    }

    // IFunction<T> & IFunction

    async ValueTask<IComputed> IFunction.Invoke(ComputedInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
    {
        if (!ReferenceEquals(input, this))
            // This "Function" supports just a single input == Input
            throw new ArgumentOutOfRangeException(nameof(input));

        return await Invoke(this, usedBy, context, cancellationToken).ConfigureAwait(false);
    }

    ValueTask<Computed<T>> IFunction<T>.Invoke(
        ComputedInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
    {
        if (!ReferenceEquals(input, this))
            // This "Function" supports just a single input == Input
            throw new ArgumentOutOfRangeException(nameof(input));

        return Invoke(this, usedBy, context, cancellationToken);
    }

    private async ValueTask<Computed<T>> Invoke(
        Replica<T> input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
    {
        context ??= ComputeContext.Current;

        var result = Computed;
        if (result.TryUseExisting(context, usedBy))
            return result;

        // No async locking here b/c RequestUpdate is, in fact, doing this
        await RequestUpdate(cancellationToken).ConfigureAwait(false);
        result = Computed;
        result.UseNew(context, usedBy);
        return result;
    }

    Task IFunction.InvokeAndStrip(ComputedInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
    {
        if (!ReferenceEquals(input, this))
            // This "Function" supports just a single input == Input
            throw new ArgumentOutOfRangeException(nameof(input));

        return InvokeAndStrip(this, usedBy, context, cancellationToken);
    }

    Task<T> IFunction<T>.InvokeAndStrip(
        ComputedInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
    {
        if (!ReferenceEquals(input, this))
            // This "Function" supports just a single input == Input
            throw new ArgumentOutOfRangeException(nameof(input));

        return InvokeAndStrip(this, usedBy, context, cancellationToken);
    }

    private Task<T> InvokeAndStrip(
        Replica<T> input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
    {
        context ??= ComputeContext.Current;

        var result = Computed;
        return result.TryUseExisting(context, usedBy)
            ? result.StripToTask(context)
            : TryRecompute(usedBy, context, cancellationToken);
    }

    // Private methods

    private Task<Unit> CreateUpdateRequestTask()
        => TaskSource.New<Unit>(true).Task;

    private void ReplaceComputedUnsafe(
        ReplicaComputed<T>? oldComputed,
        Result<T>? output, LTag version, bool isConsistent)
    {
        oldComputed?.Invalidate();
        if (output.HasValue) {
            var newComputed = new ReplicaComputed<T>(
                ComputedOptions, this, output.GetValueOrDefault(), version, isConsistent);
            if (isConsistent)
                ComputedRegistry.Instance.Register(newComputed);
            _computed = newComputed;
        }
    }

    private async Task<T> TryRecompute(
        IComputed? usedBy, ComputeContext context,
        CancellationToken cancellationToken)
    {
        // No async locking here b/c RequestUpdate is, in fact, doing this
        await RequestUpdate(cancellationToken).ConfigureAwait(false);
        var result = Computed;
        result.UseNew(context, usedBy);
        return result.Value;
    }
}
