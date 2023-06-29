using Stl.Fusion.Interception;
using Stl.Rpc.Caching;

namespace Stl.Fusion.Client.Caching;

public interface IClientComputedCache
{
    Task WhenInitialized { get; }

    ValueTask<(T Value, TextOrBytes Data)?> Get<T>(
        ComputeMethodInput input, RpcCacheKey key, CancellationToken cancellationToken);
    ValueTask<TextOrBytes?> Get(RpcCacheKey key, CancellationToken cancellationToken = default);

    void Set(RpcCacheKey key, TextOrBytes value);
    void Remove(RpcCacheKey key);
    Task Clear(CancellationToken cancellationToken = default);
}
