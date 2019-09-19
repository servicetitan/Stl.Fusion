using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Time
{
    public interface IClock
    {
        Moment Now { get; }
        Moment HighResolutionNow { get; }
        
        Moment ToRealTime(Moment localTime);
        Moment ToLocalTime(Moment realTime);
        TimeSpan ToRealTime(TimeSpan localDuration);
        TimeSpan ToLocalTime(TimeSpan realDuration);

        Task DelayAsync(TimeSpan dueIn, CancellationToken cancellationToken = default);
    }
}
