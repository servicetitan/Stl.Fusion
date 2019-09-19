using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Time.Clocks
{
    [Serializable]
    public sealed class RealTimeClock : IClock
    {
        private static readonly DateTime StopwatchZero = DateTime.UtcNow;
        private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

        public static readonly IClock Instance = new RealTimeClock();
        public static Moment Now => Instance.Now;
        public static Moment HighResolutionNow => Instance.HighResolutionNow;

        Moment IClock.Now => DateTime.UtcNow;
        Moment IClock.HighResolutionNow => (StopwatchZero + Stopwatch.Elapsed).ToMoment();
        
        public override string ToString() => $"{GetType().Name}()";

        public Moment ToRealTime(Moment localTime) => localTime; 
        public Moment ToLocalTime(Moment realTime) => realTime;
        public TimeSpan ToRealTime(TimeSpan localDuration) => localDuration;
        public TimeSpan ToLocalTime(TimeSpan realDuration) => realDuration;

        public CancellationToken SettingsChangedToken => CancellationToken.None;

        public Task Delay(Moment dueAt, CancellationToken cancellationToken = default)
        {
            var delta = dueAt - Now;
            if (delta < TimeSpan.Zero)
                delta = TimeSpan.Zero;
            return Task.Delay(delta, cancellationToken);
        }
    }
}
