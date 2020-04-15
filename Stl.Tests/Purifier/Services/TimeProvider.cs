using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.Purifier;
using Stl.Time;

namespace Stl.Tests.Purifier.Services
{
    public interface ITimeProvider
    {
        Moment GetTime();
        ValueTask<IComputed<Moment>> GetTimeAsync(CancellationToken cancellationToken = default);
        ValueTask<IComputed<Moment>> GetTimerAsync(TimeSpan offset, CancellationToken cancellationToken = default);
    }

    public class TimeProvider : ITimeProvider
    {
        protected ILogger Log { get; }

        public TimeProvider(ILogger<TimeProvider>? log = null)
        {
            Log = log as ILogger ?? NullLogger.Instance;
        }

        public Moment GetTime()
        {
            var now = RealTimeClock.Now;
            Log.LogInformation($"GetTime() -> {now}");
            return now;
        }

        public virtual ValueTask<IComputed<Moment>> GetTimeAsync(CancellationToken cancellationToken)
        {
            var computed = Computed.Current<Moment>();
#pragma warning disable 4014
            Task.Delay(250).ContinueWith(t => computed.Invalidate());
#pragma warning restore 4014
            computed.SetOutput(GetTime());
            return ValueTaskEx.FromResult(computed);
        }

        public virtual async ValueTask<IComputed<Moment>> GetTimerAsync(TimeSpan offset, CancellationToken cancellationToken)
        {
            var cNow = await GetTimeAsync(cancellationToken).ConfigureAwait(false);
            return Computed.Return(cNow.Value + offset);
        }
    }
}
