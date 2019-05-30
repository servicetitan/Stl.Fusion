using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Internal;

namespace Stl.Async
{
    public interface IAsyncComputeOnce<in TKey, TValue>
    {
        ValueTask<TValue> GetOrComputeAsync(TKey key, CancellationToken cancellationToken = default);
        IAsyncEnumerable<TValue> GetOrComputeAsync(
            IAsyncEnumerable<TKey> keys,
            [EnumeratorCancellation] CancellationToken cancellationToken = default);
    }

    public abstract class AsyncComputeOnceBase<TKey, TOut> : IAsyncComputeOnce<TKey, TOut>
    {
        protected AsyncLocal<ImmutableHashSet<TKey>> Dependents { get; set; }= 
            new AsyncLocal<ImmutableHashSet<TKey>>(); 
        protected ConcurrentDictionary<TKey, (Task<TOut>? Task, Result<TOut> Result)> Cache { get; set; }=
            new ConcurrentDictionary<TKey, (Task<TOut>? Task, Result<TOut> Result)>();

        protected abstract ValueTask<TOut> ComputeAsync(TKey key, CancellationToken cancellationToken = default);

        public ValueTask<TOut> GetOrComputeAsync(TKey key, CancellationToken cancellationToken = default)
        {
            if (Cache.TryGetValue(key, out var cached))
                return Unwrap(cached);
            return Unwrap(Cache.GetOrAdd(key, (k, s) => 
                (ComputeAndUpdateAsync(s.self, k, s.cancellationToken), default!), 
                (self: this, cancellationToken)));
        }

        public async IAsyncEnumerable<TOut> GetOrComputeAsync(
            IAsyncEnumerable<TKey> keys, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var key in keys)
                yield return await GetOrComputeAsync(key, cancellationToken).ConfigureAwait(false);
        }

        protected async ValueTask<TOut> SafeComputeAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var dependents = Dependents.Value;
            if (dependents.Contains(key))
                throw Errors.CircularDependency(key);
            Dependents.Value = dependents.Add(key);
            try {
                return await ComputeAsync(key, cancellationToken).ConfigureAwait(false);
            }
            finally {
                Dependents.Value = Dependents.Value.Remove(key);
            }
        }

        private static ValueTask<TOut> Unwrap((Task<TOut> Task, Result<TOut> Result) entry) =>
            entry.Task?.ToValueTask() ?? entry.Result;
            
        private static async Task<TOut> ComputeAndUpdateAsync(
            AsyncComputeOnceBase<TKey, TOut> self,
            TKey key,
            CancellationToken cancellationToken) 
        {
            var r = await self.SafeComputeAsync(key, cancellationToken).ConfigureAwait(false);
            self.Cache[key] = ((Task<TOut>) null, r);
            return r;
        }
    }

    public class AsyncComputeOnce<TKey, TOut> : AsyncComputeOnceBase<TKey, TOut>
    {
        public Func<TKey, CancellationToken, ValueTask<TOut>> Computer { get; }

        protected AsyncComputeOnce(Func<TKey, CancellationToken, ValueTask<TOut>> computer) => 
            Computer = computer;

        protected override ValueTask<TOut> ComputeAsync(TKey key, CancellationToken cancellationToken = default) => 
            Computer.Invoke(key, cancellationToken);
    }
}
