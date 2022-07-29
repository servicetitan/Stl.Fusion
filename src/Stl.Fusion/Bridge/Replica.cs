using Stl.Fusion.Internal;

namespace Stl.Fusion.Bridge;

public interface IReplica : IDisposable
{
    ComputedOptions ComputedOptions { get; }
    PublicationRef PublicationRef { get; }
    IReplicator Replicator { get; }
    IReplicaComputed Computed { get; }
    bool IsUpdateRequested { get; }
    Exception? UpdateError { get; }

    Task RequestUpdate(CancellationToken cancellationToken = default);
}

public interface IReplica<T> : IReplica
{
    new IReplicaComputed<T> Computed { get; }
}

public interface IReplicaImpl : IReplica, IFunction
{
    void DisposeTemporaryInstance();
    bool ApplyFailedUpdate(Exception? error, CancellationToken cancelledToken);
}

public interface IReplicaImpl<T> : IReplica<T>, IFunction<ReplicaInput, T>, IReplicaImpl
{
    bool ApplySuccessfulUpdate(Result<T>? output, LTag version, bool isConsistent);
}

public class Replica<T> : IReplicaImpl<T>
{
    protected readonly ReplicaInput Input;
    protected volatile IReplicaComputed<T> ComputedField = null!;
    protected volatile Exception? UpdateErrorField;
    protected volatile Task<Unit>? UpdateRequestTask;
    protected IReplicatorImpl ReplicatorImpl => (IReplicatorImpl) Replicator;
    protected readonly object Lock = new();

    public ComputedOptions ComputedOptions { get; }
    public PublicationRef PublicationRef => Input.PublicationRef;
    public IReplicator Replicator { get; }
    public IReplicaComputed<T> Computed => ComputedField;
    public bool IsUpdateRequested => UpdateRequestTask != null;
    public Exception? UpdateError => UpdateErrorField;

    // Explicit property implementations
    IServiceProvider IHasServices.Services => ReplicatorImpl.Services;
    IReplicaComputed IReplica.Computed => ComputedField;

    public Replica(
        ComputedOptions computedOptions,
        PublicationStateInfo<T> info,
        IReplicator replicator,
        bool isUpdateRequested = false)
    {
        if (computedOptions.SwappingOptions.IsEnabled)
            throw Errors.UnsupportedComputedOptions(GetType());
        ComputedOptions = computedOptions;
        Replicator = replicator;
        Input = new ReplicaInput(this, info.PublicationRef);
        // ReSharper disable once VirtualMemberCallInConstructor
        ApplySuccessfulUpdate(info.Output, info.Version, info.IsConsistent);
        if (isUpdateRequested)
            // ReSharper disable once VirtualMemberCallInConstructor
            UpdateRequestTask = CreateUpdateRequestTask();
    }

    // We want to make sure the replicas are connected to
    // publishers only while they're used.
#pragma warning disable MA0055
    ~Replica() => Dispose(false);
#pragma warning restore MA0055

    // Called for temp. replicas that were never exposed by ReplicaRegistry
    void IReplicaImpl.DisposeTemporaryInstance()
        => GC.SuppressFinalize(this);

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        try {
            Input.ReplicatorImpl.OnReplicaDisposed(this);
        }
        catch {
            // Intended
        }
        ReplicaRegistry.Instance.Remove(this);
    }

    Task IReplica.RequestUpdate(CancellationToken cancellationToken)
        => RequestUpdate(cancellationToken);
    public virtual Task RequestUpdate(CancellationToken cancellationToken = default)
    {
        var updateRequestTask = UpdateRequestTask;
        if (updateRequestTask != null)
            return updateRequestTask.WaitAsync(cancellationToken);
        // Double check locking
        lock (Lock) {
            updateRequestTask = UpdateRequestTask;
            if (updateRequestTask != null)
                return updateRequestTask.WaitAsync(cancellationToken);
            UpdateRequestTask = updateRequestTask = CreateUpdateRequestTask();
            Input.ReplicatorImpl.Subscribe(this);
            return updateRequestTask.WaitAsync(cancellationToken);
        }
    }

    bool IReplicaImpl<T>.ApplySuccessfulUpdate(Result<T>? output, LTag version, bool isConsistent)
        => ApplySuccessfulUpdate(output, version, isConsistent);
    protected virtual bool ApplySuccessfulUpdate(Result<T>? output, LTag version, bool isConsistent)
    {
        Task<Unit>? updateRequestTask;
        lock (Lock) {
            // 1. Update Computed & UpdateError
            UpdateErrorField = null;
            var oldComputed = ComputedField;

            if (oldComputed == null! || oldComputed.Version != version)
                ReplaceComputedUnsafe(oldComputed, output, version, isConsistent);
            else if (oldComputed.IsConsistent() != isConsistent) {
                if (isConsistent)
                    ReplaceComputedUnsafe(oldComputed, output, version, isConsistent);
                else
                    oldComputed?.Invalidate();
            }

            // 2. Complete UpdateRequestTask
            (updateRequestTask, UpdateRequestTask) = (UpdateRequestTask, null);
        }

        if (updateRequestTask != null) {
            var updateRequestTaskSource = TaskSource.For(updateRequestTask);
            updateRequestTaskSource.TrySetResult(default);
        }
        return true;
    }

    bool IReplicaImpl.ApplyFailedUpdate(Exception? error, CancellationToken cancelledToken)
        => ApplyFailedUpdate(error, cancelledToken);
    protected virtual bool ApplyFailedUpdate(Exception? error, CancellationToken cancelledToken)
    {
        IReplicaComputed<T>? computed;
        Task<Unit>? updateRequestTask;
        lock (Lock) {
            // 1. Update Computed & UpdateError
            computed = ComputedField;
            UpdateErrorField = error;

            // 2. Complete UpdateRequestTask
            (updateRequestTask, UpdateRequestTask) = (UpdateRequestTask, null);
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

    protected virtual Task<Unit> CreateUpdateRequestTask()
        => TaskSource.New<Unit>(true).Task;

    protected virtual void ReplaceComputedUnsafe(
        IReplicaComputed<T>? oldComputed,
        Result<T>? output, LTag version, bool isConsistent)
    {
        oldComputed?.Invalidate();
        if (output.HasValue) {
            var newComputed = new ReplicaComputed<T>(
                ComputedOptions, Input, output.GetValueOrDefault(), version, isConsistent);
            if (isConsistent)
                ComputedRegistry.Instance.Register(newComputed);
            ComputedField = newComputed;
        }
    }

    protected async ValueTask<IComputed<T>> Invoke(
        ReplicaInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
    {
#if DEBUG
        if (input != Input)
            // This "Function" supports just a single input == Input
            throw new ArgumentOutOfRangeException(nameof(input));
#endif

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

    protected Task<T> InvokeAndStrip(
        ReplicaInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
    {
#if DEBUG
        if (input != Input)
            // This "Function" supports just a single input == Input
            throw new ArgumentOutOfRangeException(nameof(input));
#endif

        context ??= ComputeContext.Current;

        var result = Computed;
        return result.TryUseExisting(context, usedBy)
            ? result.StripToTask(context)
            : TryRecompute(usedBy, context, cancellationToken);
    }

    protected async Task<T> TryRecompute(
        IComputed? usedBy, ComputeContext context,
        CancellationToken cancellationToken)
    {
        // No async locking here b/c RequestUpdate is, in fact, doing this
        await RequestUpdate(cancellationToken).ConfigureAwait(false);
        var result = Computed;
        result.UseNew(context, usedBy);
        return result.Value;
    }

    #region Explicit impl. of IFunction & IFunction<...>

    async ValueTask<IComputed> IFunction.Invoke(ComputedInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
        => await Invoke((ReplicaInput) input, usedBy, context, cancellationToken).ConfigureAwait(false);

    Task IFunction.InvokeAndStrip(ComputedInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
        => InvokeAndStrip((ReplicaInput) input, usedBy, context, cancellationToken);

    ValueTask<IComputed<T>> IFunction<ReplicaInput, T>.Invoke(ReplicaInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
        => Invoke(input, usedBy, context, cancellationToken);

    Task<T> IFunction<ReplicaInput, T>.InvokeAndStrip(ReplicaInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken)
        => InvokeAndStrip(input, usedBy, context, cancellationToken);

    #endregion
}
