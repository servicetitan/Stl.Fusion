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
        private readonly ISessionAccessor _sessionAccessor;

        public PerUserCounterService(ISessionAccessor sessionAccessor)
            => _sessionAccessor = sessionAccessor;

        [ComputeMethod]
        public virtual Task<int> GetAsync(string key, Session? session = null,
            CancellationToken cancellationToken = default)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            var result = _counters.TryGetValue((session.Id, key), out var value) ? value : 0;
            return Task.FromResult(result);
        }

        public Task IncrementAsync(string key, Session? session = null,
            CancellationToken cancellationToken = default)
        {
            session ??= _sessionAccessor.Session ?? throw new ArgumentNullException(nameof(session));
            _counters.AddOrUpdate((session.Id, key), k => 1, (k, v) => v + 1);
            Computed.Invalidate(() => GetAsync(key, session, default));
            return Task.CompletedTask;
        }
    }
}
