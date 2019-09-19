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
        TimeSpan ToRealTime(TimeSpan localDuration);
        TimeSpan ToLocalTime(TimeSpan realDuration);

        Task Delay(Moment dueAt, CancellationToken cancellationToken = default);
    }
}
