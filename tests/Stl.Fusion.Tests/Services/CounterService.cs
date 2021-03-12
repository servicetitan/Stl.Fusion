using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Fusion.Tests.Services
{
    public class CounterService
    {
        private readonly ConcurrentDictionary<string, int> _counters = new ConcurrentDictionary<string, int>();
        private readonly IMutableState<int> _offset;

        public CounterService(IMutableState<int> offset)
            => _offset = offset;

        [ComputeMethod(KeepAliveTime = 0.3)]
        public virtual async Task<int> Get(string key, CancellationToken cancellationToken = default)
        {
            var offset = await _offset.Use(cancellationToken).ConfigureAwait(false);
            return offset + (_counters.TryGetValue(key, out var value) ? value : 0);
        }

        public Task Increment(string key, CancellationToken cancellationToken = default)
        {
            _counters.AddOrUpdate(key, k => 1, (k, v) => v + 1);

            using (Computed.Invalidate()) {
                Get(key, default).AssertCompleted();
            }
            return Task.CompletedTask;
        }

        public Task SetOffset(int offset, CancellationToken cancellationToken = default)
        {
            _offset.Set(offset);
            return Task.CompletedTask;
        }
    }
}
