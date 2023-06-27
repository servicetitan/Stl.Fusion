using Stl.Fusion.Interception;
using Stl.Rpc;
using Stl.Rpc.Caching;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Interception;

public abstract class ClientComputedCache : RpcServiceBase
{
    public record Options
    {
        public static Options Default { get; set; } = new();

        public TimeSpan FlushDelay { get; init; } = TimeSpan.FromSeconds(0.1);
        public IMomentClock? Clock { get; init; }
    }

    protected readonly Options Settings;
    protected readonly RpcArgumentSerializer ArgumentSerializer;
    protected readonly IMomentClock Clock;

    protected readonly object Lock = new();
    protected Dictionary<RpcCacheKey, TextOrBytes?> FlushQueue = new();
    protected Task? FlushTask;

    protected ClientComputedCache(Options settings, IServiceProvider services)
        : base(services)
    {
        Settings = settings;
        ArgumentSerializer = Hub.InternalServices.ArgumentSerializer;
        Clock = settings.Clock ?? services.Clocks().CpuClock;
    }

    public async ValueTask<Option<T>> Get<T>(ComputeMethodInput input, RpcCacheKey key, CancellationToken cancellationToken)
    {
        var serviceDef = Hub.ServiceRegistry.Get(key.Service);
        if (serviceDef == null)
            return Option<T>.None;

        var methodDef = serviceDef.Get(key.Service);
        if (methodDef == null)
            return Option<T>.None;

        try {
            var resultData = await Get(key, cancellationToken).ConfigureAwait(false);
            if (resultData is not { } vResultData)
                return Option<T>.None;

            var result = methodDef.ResultListFactory.Invoke();
            ArgumentSerializer.Deserialize(ref result, methodDef.AllowResultPolymorphism, vResultData);
            return result.Get0<T>();
        }
        catch (Exception e) {
            Log.LogError(e, "Cached result read failed");
            return Option<T>.None;
        }
    }

    public ValueTask<TextOrBytes?> Get(RpcCacheKey key, CancellationToken cancellationToken)
    {
        lock (Lock) {
            if (FlushQueue.TryGetValue(key, out var value))
                return new ValueTask<TextOrBytes?>(value);
        }
        return Fetch(key, cancellationToken);
    }

    public void Set(RpcCacheKey key, TextOrBytes value)
    {
        lock (Lock) {
            FlushQueue[key] = value;
            FlushTask ??= DelayedFlush();
        }
    }

    public void Remove(RpcCacheKey key)
    {
        lock (Lock) {
            FlushQueue[key] = null;
            FlushTask ??= DelayedFlush();
        }
    }

    protected async Task DelayedFlush()
    {
        await Clock.Delay(Settings.FlushDelay).ConfigureAwait(false);
        Dictionary<RpcCacheKey, TextOrBytes?> flushQueue;
        lock (Lock) {
            flushQueue = FlushQueue;
            FlushQueue = new();
            FlushTask = null;
        }
        await Flush(flushQueue).ConfigureAwait(false);
    }

    protected abstract ValueTask<TextOrBytes?> Fetch(RpcCacheKey key, CancellationToken cancellationToken);
    protected abstract Task Flush(Dictionary<RpcCacheKey, TextOrBytes?> flushQueue);
}
