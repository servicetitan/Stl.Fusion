using Stl.Rpc.Caching;

namespace Stl.Fusion.Client.Caching;

public sealed class InMemoryClientComputedCache(
    FlushingClientComputedCache.Options settings,
    IServiceProvider services
    ) : FlushingClientComputedCache(settings, services)
{
    private static readonly ValueTask<TextOrBytes?> MissValueTask = new((TextOrBytes?)null);

    private readonly ConcurrentDictionary<RpcCacheKey, TextOrBytes> _cache = new();

    protected override ValueTask<TextOrBytes?> Fetch(RpcCacheKey key, CancellationToken cancellationToken)
        => _cache.TryGetValue(key, out var result)
            ? new ValueTask<TextOrBytes?>(result)
            : MissValueTask;

    protected override Task Flush(Dictionary<RpcCacheKey, TextOrBytes?> flushingQueue)
    {
        foreach (var (key, result) in flushingQueue) {
            if (result is { } vResult)
                _cache[key] = vResult;
            else
                _cache.Remove(key, out _);
        }
        return Task.CompletedTask;
    }

    public override async Task Clear(CancellationToken cancellationToken = default)
    {
        await Flush().ConfigureAwait(false);
        _cache.Clear();
    }
}
