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
        Task<DateTime> GetTimeAsync(int invalidateIn);
    }

    public class TimeProvider : ITimeProvider
    {
        private readonly ILogger<TimeProvider> _log;
        protected bool IsCaching { get; }

        public TimeProvider(ILogger<TimeProvider>? log = null)
        {
            _log = log ??= NullLogger<TimeProvider>.Instance;
            IsCaching = GetType().Name.EndsWith("Proxy");
        }

        private DateTime GetTime()
        {
            var now = DateTime.Now;
            _log.LogDebug($"GetTime() -> {now}");
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

        public virtual Task<DateTime> GetTimeAsync(int invalidateIn)
        {
            if (!IsCaching)
                return Task.FromResult(GetTime());
            
            var cResult = Computed.GetCurrent();
            Task.Run(async () => {
                await Task.Delay(invalidateIn).ConfigureAwait(false);
                cResult!.Invalidate();
            });
            return Task.FromResult(GetTime());
        }
    }
}
