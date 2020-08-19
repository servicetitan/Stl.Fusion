using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Caching
{
    public interface ICache
    {
        ValueTask<bool> SetAsync(
            InterceptedInput key, Option<Result<object>> value,
            TimeSpan? expirationTime, CancellationToken cancellationToken);

        [ComputeMethod(KeepAliveTime = 0)]
        ValueTask<Option<Result<object>>> GetAsync(
            InterceptedInput key, CancellationToken cancellationToken);
    }
}
