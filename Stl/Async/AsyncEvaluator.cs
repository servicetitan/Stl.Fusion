using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Async
{
    public interface IAsyncEvaluator<in TIn, TOut>
    {
        ValueTask<TOut> GetOrComputeAsync(TIn key, CancellationToken cancellationToken = default);
    }

    public abstract class AsyncEvaluatorBase<TIn, TOut> : IAsyncEvaluator<TIn, TOut>
    {
        private readonly ConcurrentDictionary<TIn, (Task<TOut>? Task, Result<TOut> Result)> _cache =
            new ConcurrentDictionary<TIn, (Task<TOut>? Task, Result<TOut> Result)>();

        public abstract ValueTask<TOut> ComputeAsync(TIn key, CancellationToken cancellationToken = default);

        public ValueTask<TOut> GetOrComputeAsync(TIn key, CancellationToken cancellationToken = default)
        {
            ValueTask<TOut> Unwrap((Task<TOut> Task, Result<TOut> Result) entry) =>
                entry.Task?.ToValueTask() ?? entry.Result;

            if (_cache.TryGetValue(key, out var cached))
                return Unwrap(cached);
            return Unwrap(_cache.GetOrAdd(key, (k, self) => {
                var computeAndUpdateTask = Task.Run(async () => {
                    var r = await self.ComputeAsync(k, cancellationToken);
                    self._cache[k] = ((Task<TOut>) null, r);
                    return r;
                }, cancellationToken);
                return (computeAndUpdateTask, default!);
            }, this));
        }
    }
}
