using System.Threading;
using System.Threading.Tasks;

namespace Stl.Caching
{
    public interface IAsyncKeyResolver<in TKey, TValue>
        where TKey : notnull
    {
        ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default);
        ValueTask<Option<TValue>> TryGetAsync(TKey key, CancellationToken cancellationToken = default);
    }

    public interface IAsyncCache<in TKey, TValue> : IAsyncKeyResolver<TKey, TValue>
        where TKey : notnull
    {
        ValueTask SetAsync(TKey key, TValue value, CancellationToken cancellationToken = default);
        ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken = default);
    }
}
