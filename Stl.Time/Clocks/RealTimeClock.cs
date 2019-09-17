using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Time.Clocks
{
    [Serializable]
    public class RealTimeClock : IClock
    {
        public static readonly RealTimeClock Instance = new RealTimeClock();

        public Moment Now => DateTime.UtcNow;
        
        public override string ToString() => $"{GetType().Name}()";

        public Moment ToRealTime(Moment localTime) => localTime; 
        public Moment ToLocalTime(Moment realTime) => realTime;
        public TimeSpan ToRealDuration(TimeSpan localDuration) => localDuration;
        public TimeSpan ToLocalDuration(TimeSpan realDuration) => realDuration;

        public Task Delay(TimeSpan duration, CancellationToken cancellationToken = default) 
            => Task.Delay(duration, cancellationToken);
        public IObservable<long> Interval(TimeSpan period) => Observable.Interval(period);
        public IObservable<long> Timer(TimeSpan dueIn) => Observable.Timer(dueIn);
    }
}