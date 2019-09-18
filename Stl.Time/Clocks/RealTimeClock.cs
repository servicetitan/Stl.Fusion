using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Time.Clocks
{
    [Serializable]
    public sealed class RealTimeClock : IClock
    {
        public static readonly IClock Instance = new RealTimeClock();
        public static Moment Now => Instance.Now;

        Moment IClock.Now => DateTime.UtcNow;
        
        public override string ToString() => $"{GetType().Name}()";

        public Moment ToRealTime(Moment localTime) => localTime; 
        public Moment ToLocalTime(Moment realTime) => realTime;
        public CancellationToken SettingsChangedToken => CancellationToken.None;

        public Task Delay(Moment dueAt, CancellationToken cancellationToken = default)
        {
            var delta = dueAt - Now;
            if (delta < TimeSpan.Zero)
                delta = TimeSpan.Zero;
            return Task.Delay(delta, cancellationToken);
        }
    }
}
