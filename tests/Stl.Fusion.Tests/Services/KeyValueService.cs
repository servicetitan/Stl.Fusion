using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Tests.Services
{
    public interface IKeyValueService<TValue>
    {
        Task<Option<TValue>> TryGetAsync(string key, CancellationToken cancellationToken = default);
        Task<TValue> GetAsync(string key, CancellationToken cancellationToken = default);
        Task SetAsync(string key, TValue value, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    }

    public class KeyValueService<TValue> : IKeyValueService<TValue>
    {
        private readonly ConcurrentDictionary<string, TValue> _values =
            new ConcurrentDictionary<string, TValue>();

        [ComputeMethod]
        public virtual Task<Option<TValue>> TryGetAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromResult(_values.TryGetValue(key, out var v) ? Option.Some(v) : default);

        [ComputeMethod]
        public virtual Task<TValue> GetAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromResult(_values.GetValueOrDefault(key)!);

        public virtual Task SetAsync(string key, TValue value, CancellationToken cancellationToken = default)
        {
            _values[key] = value;
            Computed.Invalidate(() => TryGetAsync(key, default));
            Computed.Invalidate(() => GetAsync(key, default));
            return Task.CompletedTask;
        }

        public virtual Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _values.TryRemove(key, out _);
            Computed.Invalidate(() => TryGetAsync(key, default));
            Computed.Invalidate(() => GetAsync(key, default));
            return Task.CompletedTask;
        }
    }

    [ComputeService(typeof(IKeyValueService<string>))]
    public class StringKeyValueService : KeyValueService<string> { }
}
