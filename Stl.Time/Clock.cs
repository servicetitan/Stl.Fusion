using Stl.Time.Clocks;

namespace Stl.Time
{
    public static class Clock
    {
        private static volatile IClock _current = RealTimeClock.Instance;

        public static IClock Current {
            get => _current;
            set => _current = value;
        }
    }
}
