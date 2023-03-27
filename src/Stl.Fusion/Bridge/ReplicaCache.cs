using Stl.Fusion.Interception;

namespace Stl.Fusion.Bridge;

public abstract class ReplicaCache : IHasServices
{
    protected ILogger Log { get; }

    public IServiceProvider Services { get; }

    protected ReplicaCache(IServiceProvider services)
    {
        Services = services;
        Log = services.LogFor(GetType());
    }

    public async ValueTask<Result<T>?> Get<T>(ComputeMethodInput input, CancellationToken cancellationToken)
    {
        try {
            return await GetInternal<T>(input, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) {
            Log.LogError(e, "Get({Input}) failed", input);
            return null;
        }
    }

    public async ValueTask Set<T>(ComputeMethodInput input, Result<T> output, CancellationToken cancellationToken)
    {
        try {
            await SetInternal(input, output, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) {
            Log.LogError(e, "Set({Input}, {Output}) failed", input, output);
        }
    }

    protected abstract ValueTask<Result<T>?> GetInternal<T>(ComputeMethodInput input, CancellationToken cancellationToken);
    protected abstract ValueTask SetInternal<T>(ComputeMethodInput input, Result<T> output, CancellationToken cancellationToken);
}

public sealed class NoReplicaCache : ReplicaCache
{
    public NoReplicaCache(IServiceProvider services) : base(services) { }

    protected override ValueTask<Result<T>?> GetInternal<T>(ComputeMethodInput input, CancellationToken cancellationToken)
        => ValueTaskExt.FromResult((Result<T>?)null);

    protected override ValueTask SetInternal<T>(ComputeMethodInput input, Result<T> output, CancellationToken cancellationToken) 
        => ValueTaskExt.CompletedTask;
}
