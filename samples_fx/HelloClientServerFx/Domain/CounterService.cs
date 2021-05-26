using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion;

namespace HelloClientServerFx
{
    public class CounterService : ICounterService
    {
        private readonly ConcurrentDictionary<string, int> _counters = new ConcurrentDictionary<string, int>();
        private readonly IMutableState<int> _offset;

        public CounterService(IStateFactory stateFactory)
            => _offset = stateFactory.NewMutable<int>(0);

        [ComputeMethod] // Optional: this attribute is inherited from interface
        public virtual async Task<int> Get(string key, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"{nameof(Get)}({key})");
            var offset = await _offset.Use(cancellationToken);
            return offset + (_counters.TryGetValue(key, out var value) ? value : 0);
        }

        public Task Increment(string key, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"{nameof(Increment)}({key})");
            _counters.AddOrUpdate(key, k => 1, (k, v) => v + 1);
            using (Computed.Invalidate())
                Get(key, default).Ignore();
            return Task.CompletedTask;
        }

        public Task SetOffset(int offset, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"{nameof(SetOffset)}({offset})");
            _offset.Value = offset;
            return Task.CompletedTask;
        }
    }
}