using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Caching
{
    public interface ICachingFunction<in TIn, TOut> : IFunction<TIn, TOut>
        where TIn : ComputedInput
    {
        ValueTask<Option<Result<TOut>>> GetCachedOutputAsync(
            TIn input, CacheOptions cacheOptions, CancellationToken cancellationToken = default);
    }
}
