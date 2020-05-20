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
        protected ILogger Log { get; }
        protected bool IsCaching { get; }

        public TimeProvider(ILogger<TimeProvider>? log = null)
        {
            Log = log as ILogger ?? NullLogger.Instance;
            IsCaching = GetType().Name.EndsWith("Proxy");
        }

        private DateTime GetTime()
        {
            var now = DateTime.Now;
            Log.LogDebug($"GetTime() -> {now}");
            return now;
        }

        public virtual Task<DateTime> GetTimeAsync()
        {
            if (!IsCaching)
                return Task.FromResult(GetTime());
            
            var cResult = Computed.GetCurrent();
            Task.Run(async () => {
                await Task.Delay(250).ConfigureAwait(false);
                cResult!.Invalidate();
            });
            return Task.FromResult(GetTime());
        }
    }
}
