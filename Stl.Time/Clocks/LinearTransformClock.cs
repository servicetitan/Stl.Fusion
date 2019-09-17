using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Stl.Time.Clocks
{
    [Serializable]
    public class LinearTransformClock : IClock
    {
        public IClock Origin { get; }
        public TimeSpan LocalOffset { get; }
        public TimeSpan RealOffset { get; }
        public double Multiplier { get; }

        [JsonConstructor]
        public LinearTransformClock(TimeSpan localOffset = default, TimeSpan realOffset = default, double multiplier = 1, IClock? origin = null)
        {
            Origin = origin ?? RealTimeClock.Instance;
            LocalOffset = localOffset;
            RealOffset = realOffset;
            Multiplier = multiplier;
        }
        
        public override string ToString() => $"{GetType().Name}({LocalOffset} + {Multiplier} * ({Origin} - {RealOffset}))";
        
        // Operations

        public Moment Now => ToLocalTime(Origin.Now);
        public Moment ToRealTime(Moment localTime) => new Moment(RealOffset + ToRealDuration((localTime - LocalOffset).UnixTime));
        public Moment ToLocalTime(Moment realTime) => new Moment(LocalOffset + ToLocalDuration((realTime - RealOffset).UnixTime));
        public TimeSpan ToRealDuration(TimeSpan localDuration) => localDuration / Multiplier;
        public TimeSpan ToLocalDuration(TimeSpan realDuration) => realDuration * Multiplier;

        public Task Delay(TimeSpan duration, CancellationToken cancellationToken = default) 
            => Origin.Delay(ToRealDuration(duration), cancellationToken);
        public IObservable<long> Interval(TimeSpan period) 
            => Origin.Interval(ToRealDuration(period));
        public IObservable<long> Timer(TimeSpan dueIn) 
            => Origin.Timer(ToRealDuration(dueIn));
    }
}