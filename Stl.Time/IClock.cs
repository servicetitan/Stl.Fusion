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

        CancellationToken SettingsChangedToken { get; }
        Task Delay(Moment dueAt, CancellationToken cancellationToken = default);
    }
}
