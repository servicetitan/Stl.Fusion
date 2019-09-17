using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Time.Internal;

namespace Stl.Time.Clocks
{
    [Serializable]
    public class UnusableClock : IClock
    {
        public static readonly UnusableClock Instance = new UnusableClock();

        public Moment Now => throw Errors.UnusableClock();
        
        public override string ToString() => $"{GetType().Name}()";

        public Moment ToRealTime(Moment localTime) => throw Errors.UnusableClock(); 
        public Moment ToLocalTime(Moment realTime) => throw Errors.UnusableClock();
        public TimeSpan ToRealDuration(TimeSpan localDuration) => throw Errors.UnusableClock();
        public TimeSpan ToLocalDuration(TimeSpan realDuration) => throw Errors.UnusableClock();

        public Task Delay(TimeSpan duration, CancellationToken cancellationToken = default) 
            => throw Errors.UnusableClock();
        public IObservable<long> Interval(TimeSpan period) => throw Errors.UnusableClock();
        public IObservable<long> Timer(TimeSpan dueIn) => throw Errors.UnusableClock();
    }
}