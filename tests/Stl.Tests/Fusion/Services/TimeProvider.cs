using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Fusion;

namespace Stl.Tests.Fusion.Services
{
    public interface ITimeProvider
    {
        DateTime GetTime();
        Task<DateTime> GetTimeAsync();
        Task<DateTime> GetTimeOffsetAsync(TimeSpan offset);
    }

    public class TimeProvider : ITimeProvider
    {
        protected ILogger Log { get; }
        protected bool IsCaching { get; }

        public TimeProvider(ILogger<TimeProvider>? log = null)
        {
            Log = log as ILogger ?? NullLogger.Instance;
            IsCaching = GetType().Name.EndsWith("Proxy");
        }

        public DateTime GetTime()
        {
            var now = DateTime.Now;
            Log.LogDebug($"GetTime() -> {now}");
            return now;
        }

        public virtual Task<DateTime> GetTimeAsync()
        {
            if (IsCaching) {
                // Self-invalidation
                var cResult = Computed.GetCurrent();
                Task.Run(async () => {
                    await Task.Delay(250).ConfigureAwait(false);
                    cResult!.Invalidate(
                        "Sorry, you were programmed to live for just 250ms :( " +
                        "Hopefully you enjoyed your life.");
                });
            }
            return Task.FromResult(GetTime());
        }

        public virtual async Task<DateTime> GetTimeOffsetAsync(TimeSpan offset)
        {
            var now = await GetTimeAsync().ConfigureAwait(false);
            return now + offset;
        }
    }
}
