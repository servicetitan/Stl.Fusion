using System;

namespace Stl.Time.Testing 
{
    public static class TestClockEx
    {
        public static TClock SetTo<TClock>(this TClock clock, Moment now)
            where TClock : class, ITestClock
        {
            var s = clock.Settings;
            var realNow = SystemClock.Now;
            var delta = now - s.ToLocalTime(realNow);
            clock.Settings = (s.LocalOffset + delta, s.RealOffset, s.Multiplier);
            return clock;
        }

        public static TClock OffsetBy<TClock>(this TClock clock, long offsetInMilliseconds)
            where TClock : class, ITestClock
            => clock.OffsetBy(TimeSpan.FromMilliseconds(offsetInMilliseconds));

        public static TClock OffsetBy<TClock>(this TClock clock, TimeSpan offset)
            where TClock : class, ITestClock
        {
            var s = clock.Settings;
            clock.Settings = (offset + s.LocalOffset, s.RealOffset, s.Multiplier);
            return clock;
        }

        public static TClock SpeedupBy<TClock>(this TClock clock, double multiplier)
            where TClock : class, ITestClock
        {
            var s = clock.Settings;
            var realNow = SystemClock.Now; 
            var localNow = s.ToLocalTime(realNow);
            clock.Settings = (localNow.EpochOffset, -realNow.EpochOffset, multiplier * s.Multiplier);
            return clock;
        }
    }
}
