using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Time.Internal;

namespace Stl.Time.Clocks
{
    [Serializable]
    public sealed class UnusableClock : IClock
    {
        public static readonly IClock Instance = new UnusableClock();

        public Moment Now => throw Errors.UnusableClock();
        public Moment HighResolutionNow => throw Errors.UnusableClock();

        public override string ToString() => $"{GetType().Name}()";

        public Moment ToRealTime(Moment localTime) => throw Errors.UnusableClock(); 
        public Moment ToLocalTime(Moment realTime) => throw Errors.UnusableClock();
        public TimeSpan ToRealTime(TimeSpan localDuration) => throw Errors.UnusableClock();
        public TimeSpan ToLocalTime(TimeSpan realDuration) => throw Errors.UnusableClock();

        public CancellationToken SettingsChangedToken => default;

        public Task DelayAsync(TimeSpan dueIn, CancellationToken cancellationToken = default) 
            => throw Errors.UnusableClock();
    }
}
