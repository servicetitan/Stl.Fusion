using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.Purifier;
using Stl.Time;

namespace Stl.Tests.Purifier.Services
{
    public interface ITimeProvider
    {
        ValueTask<Moment> GetTimeAsync();
    }

    public interface ITimeProviderEx : ITimeProvider
    {
        new ValueTask<IComputed<Moment>> GetTimeAsync();
    }

    public class TimeProvider : ITimeProviderEx
    {
        protected ILogger Log { get; }

        public TimeProvider(ILogger<TimeProvider>? log = null)
        {
            Log = log as ILogger ?? NullLogger.Instance;
        }

        public ValueTask<Moment> GetTimeAsync()
        {
            var now = RealTimeClock.Now;
            Log.LogInformation($"Reading RealTimeClock.Now: {now}");
            return ValueTaskEx.FromResult(now);
        }

        async ValueTask<IComputed<Moment>> ITimeProviderEx.GetTimeAsync()
        {
            if (!(Computed.Current is IComputed<Moment> c))
                throw new InvalidOperationException("Wrong Computed.Current.");
#pragma warning disable 4014
            Task.Delay(250).ContinueWith(t => c.Invalidate());
#pragma warning restore 4014
            var now = await GetTimeAsync().ConfigureAwait(false);
            return Computed.Return(now);
        }
    }
}
