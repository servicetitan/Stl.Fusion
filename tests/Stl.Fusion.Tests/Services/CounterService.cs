using System.Collections.Concurrent;

namespace Stl.Fusion.Tests.Services;

public class CounterService : IComputeService
{
    private readonly ConcurrentDictionary<string, int> _counters = new(StringComparer.Ordinal);
    private readonly IMutableState<int> _offset;

    public CounterService(IMutableState<int> offset)
        => _offset = offset;

    [ComputeMethod(MinCacheDuration = 0.3)]
    public virtual async Task<int> Get(string key, CancellationToken cancellationToken = default)
    {
        if (key.Contains("wait"))
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        if (key.Contains("fail"))
            throw new ArgumentOutOfRangeException(nameof(key));

        var offset = await _offset.Use(cancellationToken).ConfigureAwait(false);
        return offset + (_counters.TryGetValue(key, out var value) ? value : 0);
    }

    [ComputeMethod]
    public virtual async Task<int> GetFirstNonZero(string key1, string key2, CancellationToken cancellationToken = default)
    {
        var t1 = Get(key1, cancellationToken);
        var t2 = Get(key2, cancellationToken);
        var v1 = await t1.ConfigureAwait(false);
        if (v1 != 0)
            return v1;

        var v2 = await t2.ConfigureAwait(false);
        return v2;
    }

    public Task Set(string key, int value, CancellationToken cancellationToken = default)
    {
        _counters[key] = value;

        using (Computed.Invalidate())
            _ = Get(key, default).AssertCompleted();

        return Task.CompletedTask;
    }

    public Task Increment(string key, CancellationToken cancellationToken = default)
    {
        _counters.AddOrUpdate(key, k => 1, (k, v) => v + 1);

        using (Computed.Invalidate())
            _ = Get(key, default).AssertCompleted();

        return Task.CompletedTask;
    }

    public Task SetOffset(int offset, CancellationToken cancellationToken = default)
    {
        _offset.Set(offset);
        return Task.CompletedTask;
    }
}
