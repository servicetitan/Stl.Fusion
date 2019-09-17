using System;
using Stl.Time.Clocks;

namespace Stl.Time
{
    public static class ClockEx
    {
        public static Disposable<(IClock, IClock, bool)> Activate(this IClock clock, bool keepTime = false)
        {
            var oldClock = Clock.Current;
            Clock.Current = clock;
            return Disposable.New(
                state => Clock.Current = state.KeepTime 
                    ? oldClock.SetTo(state.Clock.Now) 
                    : oldClock, 
                (OldClock: oldClock, Clock: clock, KeepTime: keepTime));
        }

        public static IClock Simplify(this IClock source)
        {
            while (source is LinearTransformClock l1 && l1.Origin is LinearTransformClock l2) {
                var multiplier = l1.Multiplier * l2.Multiplier;
                var localOffset = l1.LocalOffset + l1.Multiplier * l2.LocalOffset;
                var realOffset = (l1.RealOffset * l1.Multiplier + l2.RealOffset) / multiplier;
                source = new LinearTransformClock(localOffset, realOffset, multiplier, l2.Origin);
            }
            return source;
        } 

        public static IClock SetTo(this IClock origin, Moment now)
            => new LinearTransformClock(now - origin.Now, TimeSpan.Zero, 1, origin).Simplify();

        public static IClock OffsetBy(this IClock origin, TimeSpan offset)
            => new LinearTransformClock(offset, TimeSpan.Zero, 1, origin).Simplify();

        public static IClock SpeedupBy(this IClock origin, double multiplier)
            => new LinearTransformClock(TimeSpan.Zero, TimeSpan.Zero, multiplier, origin).Simplify();
    }
}