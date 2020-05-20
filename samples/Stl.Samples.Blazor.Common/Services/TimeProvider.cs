using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Fusion;

namespace Stl.Samples.Blazor.Common.Services
{
    public interface ITimeProvider
    {
        Task<DateTime> GetTimeAsync(int invalidateIn = 1000);
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

        public virtual Task<DateTime> GetTimeAsync(int invalidateIn)
        {
            if (!IsCaching)
                return Task.FromResult(DateTime.Now);
            
            var cResult = Computed.GetCurrent();
            Task.Run(async () => {
                await Task.Delay(invalidateIn).ConfigureAwait(false);
                cResult!.Invalidate();
            });
            return Task.FromResult(DateTime.Now);
        }
    }
}
