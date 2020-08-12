using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Concurrency;

namespace Stl.Fusion.Tests.Services
{
    public interface IKeyValueService<TValue>
    {
        [Get("getValue/{key}")] // Intended
        Task<Option<TValue>> GetValueAsync([Path] string key, CancellationToken cancellationToken = default);
        [Post("setValue/{key}")] // Intended
        Task SetValueAsync([Path] string key, [Body] Option<TValue> value, CancellationToken cancellationToken = default);
    }

    public class KeyValueService<TValue> : IKeyValueService<TValue>
    {
        private readonly ConcurrentDictionary<string, TValue> _values =
            new ConcurrentDictionary<string, TValue>();

        public Task SetValueAsync(string key, Option<TValue> value, CancellationToken cancellationToken = default)
        {
            if (value.HasValue)
                _values[key] = value.UnsafeValue!;
            else
                _values.TryRemove(key, out _);
            Computed.Invalidate(() => GetValueAsync(key, default));
            return Task.CompletedTask;
        }

        [ComputeMethod]
        public virtual Task<Option<TValue>> GetValueAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromResult(
                _values.TryGetValue(key, out var v) ? Option.Some(v) : default);
    }

    [ComputeService(typeof(IKeyValueService<string>))]
    public class StringKeyValueService : KeyValueService<string> { }
}
