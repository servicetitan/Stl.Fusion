using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Time
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
        public TimeSpan ToRealDuration(TimeSpan localDuration) => localDuration;
        public TimeSpan ToLocalDuration(TimeSpan realDuration) => realDuration;

        public CancellationToken SettingsChangedToken => CancellationToken.None;

        public Task DelayAsync(TimeSpan dueIn, CancellationToken cancellationToken = default) 
            => Task.Delay(dueIn, cancellationToken);
    }
}
