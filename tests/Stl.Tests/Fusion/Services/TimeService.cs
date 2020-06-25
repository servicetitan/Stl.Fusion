using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Fusion;

namespace Stl.Tests.Fusion.Services
{
    public interface ITimeService
    {
        DateTime GetTime();
        Task<DateTime> GetTimeAsync();
        Task<DateTime> GetTimeWithOffsetAsync(TimeSpan offset);
    }

    public class TimeService : ITimeService, IComputedService
    {
        private readonly ILogger _log;
        protected bool IsCaching { get; }

        public TimeService(ILogger<TimeService>? log = null)
        {
            _log = log ??= NullLogger<TimeService>.Instance;
            IsCaching = GetType().Name.EndsWith("Proxy");
        }

        public DateTime GetTime()
        {
            var now = DateTime.Now;
            _log.LogDebug($"GetTime() -> {now}");
            return now;
        }

        [ComputedServiceMethod]
        public virtual Task<DateTime> GetTimeAsync()
        {
            if (IsCaching) {
                // Self-invalidation
                var cResult = Computed.GetCurrent();
                Task.Run(async () => {
                    await Task.Delay(250).ConfigureAwait(false);
                    cResult!.Invalidate();
                });
            }
            return Task.FromResult(GetTime());
        }

        [ComputedServiceMethod]
        public virtual async Task<DateTime> GetTimeWithOffsetAsync(TimeSpan offset)
        {
            var now = await GetTimeAsync().ConfigureAwait(false);
            return now + offset;
        }
    }
}
