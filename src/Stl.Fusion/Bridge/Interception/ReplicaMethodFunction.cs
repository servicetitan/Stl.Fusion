using Cysharp.Text;
using Stl.Fusion.Bridge.Internal;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;
using Stl.Versioning;

namespace Stl.Fusion.Bridge.Interception;

public interface IReplicaMethodFunction : IComputeMethodFunction
{
    IReplicator Replicator { get; }
    void OnInvalidated(IReplicaMethodComputed computed);
}

public class ReplicaMethodFunction<T> : ComputeFunctionBase<T>, IReplicaMethodFunction
{
    private string? _toString;

    public IReplicator Replicator { get; }
    public VersionGenerator<LTag> VersionGenerator { get; }
    public ReplicaCache ReplicaCache { get; }

    public ReplicaMethodFunction(
        ComputeMethodDef methodDef,
        IReplicator replicator,
        VersionGenerator<LTag> versionGenerator,
        ReplicaCache replicaCache)
        : base(methodDef, replicator.Services)
    {
        Replicator = replicator;
        VersionGenerator = versionGenerator;
        ReplicaCache = replicaCache;
    }

    public override string ToString()
        => _toString ??= ZString.Concat('*', base.ToString());

    public void OnInvalidated(IReplicaMethodComputed computed)
        => _ = ReplicaCache.Set<T>((ComputeMethodInput)computed.Input, null, CancellationToken.None);

    protected override async ValueTask<Computed<T>> Compute(
        ComputedInput input, Computed<T>? existing,
        CancellationToken cancellationToken)
    {
        var typedInput = (ComputeMethodInput)input;
        var typedExisting = (ReplicaMethodComputed<T>?)existing;
        if (typedExisting is not { Replica: { State: { } state } replica }) {
            // typedExisting == null: no cached computed
            // typedExisting.Replica == null: no replica
            // typedExisting.Replica.State == null: replica is disposed
            return await Compute(typedInput, typedExisting, cancellationToken).ConfigureAwait(false);
        }

        if (!state.IsConsistent) {
            // Every time we call Update on replica, it calls RequestUpdate automatically
            // if the state is consistent, which delivers invalidations.
            // But here we request an update when the state is inconsistent,
            // i.e. here we request the new data _after_ the invalidation.
            await replica.RequestUpdate().WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        var computed = replica.RenewComputed(typedInput, CreateComputed);
        if (computed == null) {
            // Two cases are possible here:
            // - Replica is there, but its State == null, i.e. it is disposed
            // - Replica.State.Output == null, i.e. somehow it didn't get any
            //   update that contains it & had no Output from the very beginning
            //   (they retain the output while possible).
            // In any of these cases all we can do is to renew it.
            return await Compute(typedInput, typedExisting, cancellationToken).ConfigureAwait(false);
        }

        ComputeContext.Current.TryCapture(computed);
        // We don't await the next call to speed up returning the result
        _ = ReplicaCache.Set<T>(typedInput, computed.Output, CancellationToken.None);
        return computed;
    }

    private Task<Computed<T>> Compute(
        ComputeMethodInput input,
        ReplicaMethodComputed<T>? existing,
        CancellationToken cancellationToken)
        => existing is { State.PublicationRef.IsNone: false } // State has valid PublicationRef -> it's an actual one
            ? RemoteCompute(input, true, cancellationToken)
            : CachedCompute(input, cancellationToken);

    private async Task<Computed<T>> CachedCompute(
        ComputeMethodInput input,
        CancellationToken cancellationToken)
    {
        var outputOpt = await ReplicaCache.Get<T>(input, cancellationToken).ConfigureAwait(false);
        if (outputOpt is not { } output)
            return await RemoteCompute(input, true, cancellationToken).ConfigureAwait(false);

        var publicationState = CreateFakePublicationState(output);
        var computed = new ReplicaMethodComputed<T>(input.MethodDef.ComputedOptions, input, null, publicationState);
        ComputeContext.Current.TryCapture(computed);

        // Start the task to retrieve the actual value
        using var _1 = ExecutionContextExt.SuppressFlow();
        _ = Task.Run(() => RemoteCompute(input, false, cancellationToken), CancellationToken.None);
        return computed;
    }

    private async Task<Computed<T>> RemoteCompute(
        ComputeMethodInput input,
        bool isCurrent,
        CancellationToken cancellationToken)
    {
        while (true) {
            var publicationState = await InvokeRemoteFunction(input, cancellationToken).ConfigureAwait(false);
            var computed = publicationState.PublicationRef.IsNone
                ? new ReplicaMethodComputed<T>(input.MethodDef.ComputedOptions, input, null, publicationState)
                : Replicator.AddOrUpdate(publicationState).RenewComputed(input, CreateComputed);
            if (computed != null) {
                if (isCurrent)
                    ComputeContext.Current.TryCapture(computed);
                // We don't await the next call to speed up returning the result
                _ = ReplicaCache.Set<T>(input, computed.Output, CancellationToken.None);
                return computed;
            }

            DebugLog?.LogError("RenewReplica: the replica was disposed right after it was created, retrying...");
        }
    }

    private async Task<PublicationStateInfo<T>> InvokeRemoteFunction(
        ComputeMethodInput input, CancellationToken cancellationToken)
    {
        Result<T> output;
        using var publicationStateCapture = new PublicationStateInfoCapture();
        try {
            var rpcResult = input.InvokeOriginalFunction(cancellationToken);
            if (input.MethodDef.ReturnsValueTask) {
                var rpcResultTask = (ValueTask<T>)rpcResult;
                output = Result.Value(await rpcResultTask.ConfigureAwait(false));
            }
            else {
                var rpcResultTask = (Task<T>)rpcResult;
                output = Result.Value(await rpcResultTask.ConfigureAwait(false));
            }
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            DebugLog?.LogError(e, "Compute: failed to fetch the initial value");
            if (e is AggregateException ae)
                e = ae.GetFirstInnerException();
            output = Result.Error<T>(e);
        }

        var publicationState = publicationStateCapture.Captured;
        if (publicationState != null)
            return new PublicationStateInfo<T>(publicationState, output);

        // No PublicationStateInfo is captured, so... 
        output = Result.Error<T>(Errors.NoPublicationStateInfo());
        return CreateFakePublicationState(output);
    }

    private PublicationStateInfo<T> CreateFakePublicationState(Result<T> output)
    {
        // A unique version tha cannot be generated by LTagGenerators
        var version = new LTag(VersionGenerator.NextVersion().Value ^ (1L << 62));
        return new PublicationStateInfo<T>(PublicationRef.None, version, true, output);
    }

    private static ReplicaMethodComputed<T> CreateComputed(
        Replica<T> replica,
        PublicationStateInfo<T> state,
        ComputeMethodInput input)
        => new (input.MethodDef.ComputedOptions, input, replica, state);
}
