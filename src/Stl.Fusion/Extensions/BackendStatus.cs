using Stl.Collections.Slim;
using Stl.Fusion.Authentication;
using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Extensions;

public class BackendStatus : IBackendStatus
{
    protected static FieldInfo UsedField { get; } = typeof(Computed<ComputeMethodInput, Unit>)
        .GetField("_used", BindingFlags.Instance | BindingFlags.NonPublic)!;
    protected IAuth Auth { get; }

    public BackendStatus(IAuth auth)
        => Auth = auth;

    public virtual async Task<Exception?> GetError(
        Session session,
        string? backendName = null,
        CancellationToken cancellationToken = default)
    {
        try {
            var backendComputed = await Computed.Capture(ct => HitBackend(session, backendName, ct), cancellationToken);
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            var usedSet = (IRefHashSetSlim<IComputedImpl>) UsedField.GetValue(backendComputed);
            var replica = usedSet.Items
                .OfType<IReplicaMethodComputed>()
                .Select(c => c.Replica)
                .FirstOrDefault(r => r != null);
            if (replica == null)
                return null; // No replica = backend is local
            var publisherConnectionState = replica.Replicator.GetPublisherConnectionState(replica.PublicationRef.PublisherId);
            await publisherConnectionState.Use(cancellationToken);
            return null;
        }
        catch (Exception ex) {
            return ex;
        }
    }

    // Protected methods

    [ComputeMethod]
    protected virtual async Task<Unit> HitBackend(
        Session session,
        string? backendName,
        CancellationToken cancellationToken = default)
    {
        await Auth.GetSessionInfo(session, cancellationToken);
        return default;
    }
}
