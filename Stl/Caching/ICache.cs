using System.Threading;
using System.Threading.Tasks;

namespace Stl.Caching
{
    public interface ICache<in TKey, TValue> : IAsyncKeyResolver<TKey, TValue>
        where TKey : notnull
    {
        ValueTask SetAsync(TKey key, TValue value, CancellationToken cancellationToken = default);
        ValueTask InvalidateAsync(TKey key, CancellationToken cancellationToken = default);
    }
}
