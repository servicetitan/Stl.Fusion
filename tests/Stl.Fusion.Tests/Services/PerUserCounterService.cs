using System.Collections.Concurrent;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Tests.Services;

public class PerUserCounterService : IComputeService
{
    private readonly ConcurrentDictionary<(string, string), int> _counters = new();

    [ComputeMethod]
    public virtual Task<int> Get(string key, Session session, CancellationToken cancellationToken = default)
    {
        var result = _counters.TryGetValue((session.Id, key), out var value) ? value : 0;
        return Task.FromResult(result);
    }

    public Task Increment(string key, Session session, CancellationToken cancellationToken = default)
    {
        _counters.AddOrUpdate((session.Id, key), _ => 1, (_, v) => v + 1);

        using (Computed.Invalidate()) {
            Get(key, session, default).AssertCompleted();
        }
        return Task.CompletedTask;
    }
}
