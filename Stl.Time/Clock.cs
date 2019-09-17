using System;
using System.Threading;
using Stl.Internal;
using Stl.Time.Clocks;

namespace Stl.Time
{
    public static class Clock
    {
        private static readonly AsyncLocal<IClock> _current = new AsyncLocal<IClock>();
        private static SetOnceRef<IClock> _defaultCurrent;

        public static IClock DefaultCurrent {
            get => _defaultCurrent.Value ?? RealTime;
            set => _defaultCurrent.Value = value;
        }

        public static IClock Current {
            get => _current.Value ?? DefaultCurrent;
            set => _current.Value = value;
        }

        public static readonly RealTimeClock RealTime = RealTimeClock.Instance;
    }
}