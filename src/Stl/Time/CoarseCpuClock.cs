using System;
using System.Reactive.PlatformServices;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Time.Internal;

namespace Stl.Time
{
    public sealed class CoarseCpuClock : IMomentClock
    {
        public static readonly IMomentClock Instance = new CoarseCpuClock();
        
        public static Moment Now {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => CoarseStopwatch.Now;
        }

        Moment IMomentClock.Now => Now;
        DateTimeOffset ISystemClock.UtcNow => Now;
        DateTimeOffset Microsoft.Extensions.Internal.ISystemClock.UtcNow => Now;
        
        private CoarseCpuClock() { }
        
        public override string ToString() => $"{GetType().Name}()";
        public Moment ToRealTime(Moment localTime) => localTime; 
        public Moment ToLocalTime(Moment realTime) => realTime;
        public TimeSpan ToRealDuration(TimeSpan localDuration) => localDuration;
        public TimeSpan ToLocalDuration(TimeSpan realDuration) => realDuration;

        public Task DelayAsync(TimeSpan dueIn, CancellationToken cancellationToken = default) 
            => Task.Delay(dueIn, cancellationToken);
    }
}
