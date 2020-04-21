using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Purifier;
using Stl.Time;

namespace Stl.Tests.Purifier.Services
{
    public interface ITimeProvider
    {
        Moment GetTime();
        Task<Moment> GetTimeAsync();
        Task<Moment> GetTimeOffsetAsync(TimeSpan offset);
    }

    public class TimeProvider : ITimeProvider
    {
        protected ILogger Log { get; }

        public TimeProvider(ILogger<TimeProvider>? log = null) 
            => Log = log as ILogger ?? NullLogger.Instance;

        public Moment GetTime()
        {
            var now = RealTimeClock.Now;
            Log.LogDebug($"GetTime() -> {now}");
            return now;
        }

        public virtual Task<Moment> GetTimeAsync()
        {
            var computed = Computed.GetCurrent();
            if (computed != null) // Otherwise there is no interception / it's a regular class
                Task.Run(async () => {
                    await Task.Delay(250).ConfigureAwait(false);
                    computed.Invalidate(
                        "Sorry, you were programmed to live for just 250ms :( " +
                        "Hopefully you enjoyed your life.");
                });
            return Task.FromResult(GetTime());
        }

        public virtual async Task<Moment> GetTimeOffsetAsync(TimeSpan offset)
        {
            var now = await GetTimeAsync().ConfigureAwait(false);
            return now + offset;
        }
    }
}
