using System;
using System.Reactive.PlatformServices;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Time
{
    public sealed class SystemClock : IMomentClock
    {
        public static readonly IMomentClock Instance = new SystemClock();

        public static Moment Now {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => DateTime.UtcNow;
        }

        Moment IMomentClock.Now => Now;
        DateTimeOffset ISystemClock.UtcNow => Now;
        DateTimeOffset Microsoft.Extensions.Internal.ISystemClock.UtcNow => Now;

        private SystemClock() { }
        
        public override string ToString() => $"{GetType().Name}()";
        public Moment ToRealTime(Moment localTime) => localTime; 
        public Moment ToLocalTime(Moment realTime) => realTime;
        public TimeSpan ToRealDuration(TimeSpan localDuration) => localDuration;
        public TimeSpan ToLocalDuration(TimeSpan realDuration) => realDuration;

        public Task DelayAsync(TimeSpan dueIn, CancellationToken cancellationToken = default) 
            // TODO: Make it work properly, i.e. taking into account time changes, sleep/resume, etc.
            => Task.Delay(dueIn, cancellationToken);
    }
}
