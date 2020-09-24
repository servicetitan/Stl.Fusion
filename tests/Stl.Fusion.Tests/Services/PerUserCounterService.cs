using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Tests.Services
{
    public class PerUserCounterService
    {
        private readonly ConcurrentDictionary<(string, string), int> _counters = new ConcurrentDictionary<(string, string), int>();

        [ComputeMethod]
        public virtual Task<int> GetAsync(string key, Session session, CancellationToken cancellationToken = default)
        {
            var result = _counters.TryGetValue((session.Id, key), out var value) ? value : 0;
            return Task.FromResult(result);
        }

        public Task IncrementAsync(string key, Session session, CancellationToken cancellationToken = default)
        {
            _counters.AddOrUpdate((session.Id, key), k => 1, (k, v) => v + 1);
            Computed.Invalidate(() => GetAsync(key, session, default));
            return Task.CompletedTask;
        }
    }
}
