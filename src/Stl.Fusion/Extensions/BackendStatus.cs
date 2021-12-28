using Stl.Collections.Slim;
using Stl.Fusion.Authentication;
using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Extensions;

public class BackendStatus : IBackendStatus
{
    private static FieldInfo UsedField { get; } = typeof(Computed<ComputeMethodInput, Unit>)
        .GetField("_used", BindingFlags.Instance | BindingFlags.NonPublic)!;
    protected IAuth Auth { get; }

    public ImmutableArray<string> Backends { get; } = ImmutableArray.Create<string>("Server");

    public BackendStatus(IAuth auth)
        => Auth = auth;

    public virtual async Task<ImmutableList<(string Backend, Exception Error)>> GetAllErrors(
        Session session,
        CancellationToken cancellationToken = default)
    {
        var result = ImmutableList<(string Backend, Exception Error)>.Empty;
        foreach (var backend in Backends) {
            var error = await GetError(session, backend, cancellationToken).ConfigureAwait(false);
            if (error != null)
                result = result.Add((backend, error));
        }
        return result;
    }

    public virtual async Task<Exception?> GetError(
        Session session,
        string backend,
        CancellationToken cancellationToken = default)
    {
        var backendComputed = await Computed
            .Capture(ct => HitBackend(session, backend, ct), cancellationToken)
            .ConfigureAwait(false);
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        var usedSet = (IRefHashSetSlim<IComputedImpl>) UsedField.GetValue(backendComputed)!;
        foreach (var usedComputed in usedSet.Items) {
            if (usedComputed is not IReplicaMethodComputed replicaMethodComputed)
                continue;
            var replica = replicaMethodComputed.Replica;
            if (replica == null)
                continue;
            try {
                var publisherId = replica.PublicationRef.PublisherId;
                var connectionState = replica.Replicator.GetPublisherConnectionState(publisherId);
                await connectionState.Use(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) {
                return e;
            }
        }
        return null;
    }

    // Protected methods

    [ComputeMethod]
    protected virtual async Task<Unit> HitBackend(
        Session session,
        string backend,
        CancellationToken cancellationToken = default)
    {
        await Auth.GetAuthInfo(session, cancellationToken).ConfigureAwait(false);
        return default;
    }
}
