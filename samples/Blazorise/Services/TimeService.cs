using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;
using Templates.Blazor2.Abstractions;

namespace Templates.Blazor2.Services
{
    [ComputeService(typeof(ITimeService))]
    public class TimeService : ITimeService
    {
        [ComputeMethod(AutoInvalidateTime = 1, KeepAliveTime = 1)]
        public virtual async Task<DateTime> GetTimeAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(250, cancellationToken).ConfigureAwait(false); // To simulate slow response
            return DateTime.Now;
        }
    }
}
