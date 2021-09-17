using System;
using System.Reactive.PlatformServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Time.Internal;

namespace Stl.Time.Testing
{
    public sealed class UnusableClock : IMomentClock
    {
        public static readonly IMomentClock Instance = new UnusableClock();

        DateTimeOffset ISystemClock.UtcNow => Now;
        public Moment Now => throw Errors.UnusableClock();
        public Moment HighResolutionNow => throw Errors.UnusableClock();

        public override string ToString() => $"{GetType().Name}()";

        public Moment ToRealTime(Moment localTime) => throw Errors.UnusableClock();
        public Moment ToLocalTime(Moment realTime) => throw Errors.UnusableClock();
        public TimeSpan ToRealDuration(TimeSpan localDuration) => throw Errors.UnusableClock();
        public TimeSpan ToLocalDuration(TimeSpan realDuration) => throw Errors.UnusableClock();

        public CancellationToken SettingsChangedToken => default;

        public Task Delay(TimeSpan dueIn, CancellationToken cancellationToken = default)
            => throw Errors.UnusableClock();
    }
}
