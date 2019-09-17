using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Time
{
    public interface IClock
    {
        Moment Now { get; }
        
        Moment ToRealTime(Moment localTime);
        Moment ToLocalTime(Moment realTime);
        TimeSpan ToRealDuration(TimeSpan localDuration);
        TimeSpan ToLocalDuration(TimeSpan realDuration);

        Task Delay(TimeSpan duration, CancellationToken cancellationToken = default);
        IObservable<long> Interval(TimeSpan period);
        IObservable<long> Timer(TimeSpan dueIn);
    }
}