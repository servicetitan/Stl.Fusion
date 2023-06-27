using System.Text;
using Stl.Fusion.Interception;
using Stl.Rpc;
using Stl.Rpc.Caching;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Caching;

public abstract class ClientComputedCache : RpcServiceBase
{
    public static RpcCacheKey VersionKey { get; set; } = new("", "Version", TextOrBytes.EmptyBytes);

    protected RpcArgumentSerializer ArgumentSerializer;

    protected ClientComputedCache(IServiceProvider services)
        : base(services)
        => ArgumentSerializer = Hub.InternalServices.ArgumentSerializer;

    public async Task CheckVersion(string version, CancellationToken cancellationToken = default)
    {
        var expectedValue = new TextOrBytes(Encoding.UTF8.GetBytes(version));
        var value = await Get(VersionKey, cancellationToken).ConfigureAwait(false);
        if (value is { } vValue) {
            if (vValue.DataEquals(expectedValue))
                return;
        }

        await Clear(cancellationToken).ConfigureAwait(false);
        Set(VersionKey, expectedValue);
        if (this is FlushingClientComputedCacheBase flushingCache)
            await flushingCache.Flush().ConfigureAwait(false);
    }

    public async ValueTask<Option<T>> Get<T>(ComputeMethodInput input, RpcCacheKey key, CancellationToken cancellationToken)
    {
        var serviceDef = Hub.ServiceRegistry.Get(key.Service);
        if (serviceDef == null)
            return Option<T>.None;

        var methodDef = serviceDef.Get(key.Method);
        if (methodDef == null)
            return Option<T>.None;

        try {
            var value = await Get(key, cancellationToken).ConfigureAwait(false);
            if (value is not { } vValue)
                return Option<T>.None;

            var resultList = methodDef.ResultListFactory.Invoke();
            ArgumentSerializer.Deserialize(ref resultList, methodDef.AllowResultPolymorphism, vValue);
            return resultList.Get0<T>();
        }
        catch (Exception e) {
            Log.LogError(e, "Cached result read failed");
            return Option<T>.None;
        }
    }

    public abstract ValueTask<TextOrBytes?> Get(RpcCacheKey key, CancellationToken cancellationToken = default);
    public abstract void Set(RpcCacheKey key, TextOrBytes value);
    public abstract void Remove(RpcCacheKey key);
    public abstract Task Clear(CancellationToken cancellationToken = default);
}
