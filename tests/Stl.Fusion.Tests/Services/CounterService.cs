using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Tests.Services
{
    public class CounterService
    {
        private readonly ConcurrentDictionary<string, int> _counters = new ConcurrentDictionary<string, int>();
        private readonly IMutableState<int> _offset;

        public CounterService(IStateFactory stateFactory)
            => _offset = stateFactory.NewMutable<int>();

        [ComputeMethod]
        public virtual async Task<int> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            var offset = await _offset.UseAsync(cancellationToken).ConfigureAwait(false);
            return offset + (_counters.TryGetValue(key, out var value) ? value : 0);
        }

        public Task IncrementAsync(string key, CancellationToken cancellationToken = default)
        {
            _counters.AddOrUpdate(key, k => 1, (k, v) => v + 1);
            Computed.Invalidate(() => GetAsync(key, default));
            return Task.CompletedTask;
        }

        public Task SetOffsetAsync(int offset, CancellationToken cancellationToken = default)
            => _offset.SetAsync(offset, cancellationToken).AsTask();
    }}
