using Stl.Rpc.Caching;

namespace Stl.Fusion.Client.Caching;

public class SharedClientComputedCache : ClientComputedCache
{
    public static ClientComputedCache Instance { get; private set; } = null!;

    public SharedClientComputedCache(ClientComputedCache instance)
        : base(instance.Settings, instance.Services)
    {
        Instance = instance;
        WhenInitialized = instance.WhenInitialized;
    }

    public override ValueTask<TextOrBytes?> Get(RpcCacheKey key, CancellationToken cancellationToken = default)
        => Instance.Get(key, cancellationToken);

    public override void Set(RpcCacheKey key, TextOrBytes value)
        => Instance.Set(key, value);

    public override void Remove(RpcCacheKey key)
        => Instance.Remove(key);

    public override Task Clear(CancellationToken cancellationToken = default)
        => Instance.Clear(cancellationToken);
}
