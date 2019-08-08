using System.Threading;
using System.Threading.Tasks;
using Optional;

namespace Stl.Caching
{
    public interface IReadOnlyCache<in TKey, TValue>
        where TKey : notnull
    {
        ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default);
        ValueTask<Option<TValue>> TryGetAsync(TKey key, CancellationToken cancellationToken = default);
    }

    public interface ICache<in TKey, TValue> : IReadOnlyCache<TKey, TValue>
        where TKey : notnull
    {
        ValueTask SetAsync(TKey key, TValue value, CancellationToken cancellationToken = default);
        ValueTask InvalidateAsync(TKey key, CancellationToken cancellationToken = default);
    }
}
