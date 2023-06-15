using Stl.Fusion.Interception;

namespace Stl.Fusion.Client.Cache;

public abstract class ClientComputedCache : IHasServices
{
    protected ILogger Log { get; }

    public IServiceProvider Services { get; }

    protected ClientComputedCache(IServiceProvider services)
    {
        Services = services;
        Log = services.LogFor(GetType());
    }

    public async ValueTask<Result<T>?> Get<T>(ComputeMethodInput input, CancellationToken cancellationToken)
    {
        try {
            var clientCacheBehavior = input.MethodDef.ComputedOptions.ClientCacheBehavior;
            switch (clientCacheBehavior) {
                case ClientCacheBehavior.None:
                    return null;
                case ClientCacheBehavior.DefaultValue:
                    return Result.New(default(T)!);
            }

            var output = await GetInternal<T>(input, cancellationToken).ConfigureAwait(false);
            if (!output.HasValue && clientCacheBehavior == ClientCacheBehavior.DefaultValueOnMiss)
                return Result.New(default(T)!);

            return output;
        }
        catch (Exception e) {
            Log.LogError(e, "Get({Input}) failed", input);
            return null;
        }
    }

    public async ValueTask Set<T>(ComputeMethodInput input, Result<T>? output, CancellationToken cancellationToken)
    {
        try {
            var cacheBehavior = input.MethodDef.ComputedOptions.ClientCacheBehavior;
            switch (cacheBehavior) {
                case ClientCacheBehavior.None:
                    return;
                case ClientCacheBehavior.DefaultValue:
                    return;
            }

            await SetInternal(input, output, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) {
            Log.LogError(e, "Set({Input}, {Output}) failed", input, output);
        }
    }

    protected abstract ValueTask<Result<T>?> GetInternal<T>(ComputeMethodInput input, CancellationToken cancellationToken);
    protected abstract ValueTask SetInternal<T>(ComputeMethodInput input, Result<T>? output, CancellationToken cancellationToken);
}
