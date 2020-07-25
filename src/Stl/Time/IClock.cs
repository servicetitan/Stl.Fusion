using System;
using System.Reactive.PlatformServices;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Time
{
    public interface IMomentClock : ISystemClock, Microsoft.Extensions.Internal.ISystemClock
    {
        Moment Now { get; }

        Moment ToRealTime(Moment localTime);
        Moment ToLocalTime(Moment realTime);
        TimeSpan ToRealDuration(TimeSpan localDuration);
        TimeSpan ToLocalDuration(TimeSpan realDuration);

        Task DelayAsync(TimeSpan dueIn, CancellationToken cancellationToken = default);
    }
}
