namespace Stl.Fusion.Bridge;

public abstract class Replica : IEquatable<Replica>, IDisposable
{
    protected readonly IReplicatorImpl ReplicatorImpl;
    protected readonly int HashCode;

    public PublicationRef PublicationRef { get; }
    public IReplicator Replicator => ReplicatorImpl;
    public bool IsDisposed => UntypedState == null;

    public abstract PublicationStateInfo? UntypedState { get; }
    public abstract bool IsUpdateRequested { get; }

    protected Replica(PublicationRef publicationRef, IReplicatorImpl replicatorImpl)
    {
        PublicationRef = publicationRef;
        ReplicatorImpl = replicatorImpl;
        HashCode = PublicationRef.GetHashCode();
    }

    public override string ToString()
        => $"{GetType().GetName()}({PublicationRef})";

    // Called for temp. replicas that were never exposed by ReplicaRegistry
    public void DisposeTemporaryInstance()
        => GC.SuppressFinalize(this);

    // Abstract members
    public abstract void Dispose();
    public abstract Task RequestUpdateUntyped(bool force = false);
    public abstract void UpdateUntyped(PublicationStateInfo? state);

    // Equality

    public bool Equals(Replica? other)
        => !ReferenceEquals(null, other) && PublicationRef == other.PublicationRef;
    public override bool Equals(object? obj)
        => Equals(obj as Replica);
    public override int GetHashCode()
        => HashCode;
}

public sealed class Replica<T> : Replica
{
    private static readonly TaskCompletionSource<PublicationStateInfo<T>?> NullStateUpdateRequestSource =
        TaskCompletionSourceExt.New<PublicationStateInfo<T>?>().WithResult(null);

    private volatile TaskCompletionSource<PublicationStateInfo<T>?>? _updateRequestSource;
    private readonly object _lock = new();
    private volatile PublicationStateInfo<T>? _state;

    public Computed<T>? Computed { get; private set; }
    public override PublicationStateInfo? UntypedState => _state;
    public PublicationStateInfo<T>? State => _state;
    public override bool IsUpdateRequested => _updateRequestSource != null;

    public Replica(PublicationStateInfo<T> state, IReplicatorImpl replicatorImpl)
        : base(state.PublicationRef, replicatorImpl)
        => _state = state;

    public override void Dispose()
    {
        if (State != null)
            Update(null); // Dispose is handled via Update
    }

    // We want to make sure the replicas are connected to
    // publishers only while they're used.
#pragma warning disable MA0055
    ~Replica() => Dispose();
#pragma warning restore MA0055

    public TComputed? RenewComputed<TArg, TComputed>(
        TArg arg, 
        Func<Replica<T>, PublicationStateInfo<T>, TArg, TComputed> computedFactory)
        where TComputed : Computed<T>
    {
        Computed<T>? oldComputed;
        Computed<T>? computed;
        lock (_lock) {
            oldComputed = Computed;
            var state = State;
            Computed = computed = state?.Output == null ? null : computedFactory.Invoke(this, state, arg);
        }
        oldComputed?.Invalidate();
        return (TComputed?)computed;
    }

    public override Task RequestUpdateUntyped(bool force = false)
        => RequestUpdate(force);
    public Task<PublicationStateInfo<T>?> RequestUpdate(bool force = false)
    {
        var updateRequestSource = _updateRequestSource;
        if (updateRequestSource != null && !force)
            return updateRequestSource.Task;

        var mustSubscribe = force;
        // Double check locking
        lock (_lock) {
            updateRequestSource = _updateRequestSource;
            if (updateRequestSource == null) {
                if (State != null) {
                    updateRequestSource = _updateRequestSource = TaskCompletionSourceExt.New<PublicationStateInfo<T>?>();
                    mustSubscribe = true;
                }
                else {
                    updateRequestSource = _updateRequestSource = NullStateUpdateRequestSource;
                    mustSubscribe = false;
                }
            }
        }
        if (mustSubscribe)
            _ = ReplicatorImpl.Subscribe(this);
        return updateRequestSource.Task;
    }

    public override void UpdateUntyped(PublicationStateInfo? state)
        => Update((PublicationStateInfo<T>?) state);
    public void Update(PublicationStateInfo<T>? state)
    {
        PublicationStateInfo<T>? oldState;
        TaskCompletionSource<PublicationStateInfo<T>?>? oldUpdateRequestSource;
        lock (_lock) {
            oldState = _state;
            if (oldState == null) // Already disposed - we do "dispose just once" check here 
                return;

            if (state is { Output: null }) {
                // SubscriptionProcessor sends the output only when it knows
                // its version differs from the last one seen by Replica.
                // See SubscriptionProcessor.TrySendUpdate, line ~ 150
                // So if there in no Output, we try to preserve the old one.
                state.Output = oldState.Output;
            }
            _state = state;
            (oldUpdateRequestSource, _updateRequestSource) = (_updateRequestSource, null);
        }

        var isChanged = state == null
            || state.Version != oldState.Version
            || state.IsConsistent != oldState.IsConsistent;
        if (isChanged)
            Computed?.Invalidate();

        if (oldUpdateRequestSource != null && oldUpdateRequestSource != NullStateUpdateRequestSource)
            oldUpdateRequestSource.TrySetResult(state);

        // This is, in fact, Dispose logic
        if (state == null) {
            GC.SuppressFinalize(this);
            ReplicaRegistry.Instance.Unregister(this); // It's already unavailable there once State is set
            ReplicatorImpl.OnReplicaDisposed(this);
            return;
        }

        if (isChanged && state.IsConsistent)
            RequestUpdate(); // Any consistent replica should auto-subscribe for invalidation messages
    }
}
