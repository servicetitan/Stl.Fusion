using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Stl.Fusion.Tests.Services
{
    public interface ITimeService
    {
        DateTime GetTime();
        [ComputeMethod]
        Task<DateTime> GetTimeAsync(CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<DateTime> GetTimeWithOffsetAsync(TimeSpan offset);
    }

    [ComputeService(typeof(ITimeService))]
    public class TimeService : ITimeService
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

        [ComputeMethod(AutoInvalidateTime = 0.25)]
        public virtual Task<DateTime> GetTimeAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(GetTime());

        public virtual async Task<DateTime> GetTimeWithOffsetAsync(TimeSpan offset)
        {
            var now = await GetTimeAsync().ConfigureAwait(false);
            return now + offset;
        }
    }
}
