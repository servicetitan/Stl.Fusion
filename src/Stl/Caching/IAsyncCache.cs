using System.Threading;
using System.Threading.Tasks;

namespace Stl.Caching
{
    public interface IAsyncKeyResolver<in TKey, TValue>
        where TKey : notnull
    {
        ValueTask<TValue> Get(TKey key, CancellationToken cancellationToken = default);
        ValueTask<Option<TValue>> TryGet(TKey key, CancellationToken cancellationToken = default);
    }

    public interface IAsyncCache<in TKey, TValue> : IAsyncKeyResolver<TKey, TValue>
        where TKey : notnull
    {
        ValueTask Set(TKey key, TValue value, CancellationToken cancellationToken = default);
        ValueTask Remove(TKey key, CancellationToken cancellationToken = default);
    }
}
