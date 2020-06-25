using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Fusion;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Server.Services
{
    public class TimeService : ITimeService, IComputedService
    {
        private readonly ILogger _log;

        public TimeService(ILogger<TimeService>? log = null) 
            => _log = log ??= NullLogger<TimeService>.Instance;

        [ComputedServiceMethod]
        public virtual Task<DateTime> GetTimeAsync(CancellationToken cancellationToken = default)
        {
            var cResult = Computed.GetCurrent();
            Task.Run(async () => {
                // This method is fancy: it self-invalidates its own result
                await Task.Delay(100, CancellationToken.None).ConfigureAwait(false);
                cResult!.Invalidate();
            });
            return Task.FromResult(DateTime.Now);
        }
    }
}
