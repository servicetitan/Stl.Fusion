using System;
using System.Reactive.PlatformServices;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Time
{
    public interface IMomentClock : ISystemClock
    {
        Moment Now { get; }

        Moment ToRealTime(Moment localTime);
        Moment ToLocalTime(Moment realTime);
        TimeSpan ToRealDuration(TimeSpan localDuration);
        TimeSpan ToLocalDuration(TimeSpan realDuration);

        Task Delay(TimeSpan dueIn, CancellationToken cancellationToken = default);
    }
}
