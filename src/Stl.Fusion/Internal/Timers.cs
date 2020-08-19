using System;
using Stl.OS;
using Stl.Time;

namespace Stl.Fusion.Internal
{
    public static class Timers
    {
        public readonly static ConcurrentTimerSet<object> KeepAlive;
        public readonly static ConcurrentTimerSet<IComputed> ReleaseOutput;
        public readonly static IMomentClock Clock;

        static Timers()
        {
            Clock = CoarseCpuClock.Instance;
            var concurrencyLevel = HardwareInfo.ProcessorCountPo2 << 4;
            KeepAlive = new ConcurrentTimerSet<object>(
                new ConcurrentTimerSet<object>.Options() {
                    Quanta = TimeSpan.FromMilliseconds(250),
                    ConcurrencyLevel = concurrencyLevel,
                    Clock = Clock,
                });
            ReleaseOutput = new ConcurrentTimerSet<IComputed>(
                new ConcurrentTimerSet<IComputed>.Options() {
                    Quanta = TimeSpan.FromSeconds(1),
                    ConcurrencyLevel = concurrencyLevel,
                    Clock = Clock,
                    FireHandler = t => t.ReleaseOutput(),
                });
        }
    }
}
