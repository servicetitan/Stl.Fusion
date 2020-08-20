using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Caching
{
    public interface ICache<in TKey>
        where TKey : notnull
    {
        ValueTask SetAsync(
            TKey key, object value, TimeSpan expirationTime, CancellationToken cancellationToken);
        ValueTask RemoveAsync(
            TKey key, CancellationToken cancellationToken);

        [ComputeMethod(KeepAliveTime = 0)]
        ValueTask<Option<object>> GetAsync(
            TKey key, CancellationToken cancellationToken);
    }
}
