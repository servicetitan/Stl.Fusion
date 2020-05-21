using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Fusion;

namespace Stl.Samples.Blazor.Common.Services
{
    public interface ITimeProvider
    {
        Task<DateTime> GetTimeAsync();
    }

    public class TimeProvider : ITimeProvider
    {
        private readonly ILogger<TimeProvider> _log;

        public TimeProvider(ILogger<TimeProvider>? log = null) 
            => _log = log ??= NullLogger<TimeProvider>.Instance;

        public virtual Task<DateTime> GetTimeAsync()
        {
            var cResult = Computed.GetCurrent();
            Task.Run(async () => {
                // This method is fancy: it self-invalidates its own result
                await Task.Delay(100).ConfigureAwait(false);
                cResult!.Invalidate();
            });
            return Task.FromResult(DateTime.Now);
        }
    }
}
