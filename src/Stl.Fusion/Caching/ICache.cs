using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Caching
{
    public interface ICache
    {
        ValueTask SetAsync(
            InterceptedInput key, Result<object> value, TimeSpan expirationTime, CancellationToken cancellationToken);
        ValueTask RemoveAsync(
            InterceptedInput key, CancellationToken cancellationToken);

        [ComputeMethod(KeepAliveTime = 0)]
        ValueTask<Option<Result<object>>> GetAsync(
            InterceptedInput key, CancellationToken cancellationToken);
    }
}
