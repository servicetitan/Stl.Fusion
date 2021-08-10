using System;
using Stl.Fusion.Swapping;
using Stl.OS;
using Stl.Time;

namespace Stl.Fusion.Internal
{
    public static class Timeouts
    {
        public static readonly ConcurrentTimerSet<object> KeepAlive;
        public static readonly ConcurrentTimerSet<ISwappable> Swap;
        public static readonly ConcurrentTimerSet<IComputed> Invalidate;
        public static readonly IMomentClock Clock;

        static Timeouts()
        {
            Clock = MomentClockSet.Default.CpuClock;
            var concurrencyLevel = HardwareInfo.GetProcessorCountPo2Factor(8);
            KeepAlive = new ConcurrentTimerSet<object>(
                new ConcurrentTimerSet<object>.Options() {
                    Quanta = TimeSpan.FromMilliseconds(250),
                    ConcurrencyLevel = concurrencyLevel,
                    Clock = Clock,
                });
            Swap = new ConcurrentTimerSet<ISwappable>(
                new ConcurrentTimerSet<ISwappable>.Options() {
                    Quanta = TimeSpan.FromMilliseconds(250),
                    ConcurrencyLevel = concurrencyLevel,
                    Clock = Clock,
                },
                t => t.Swap());
            Invalidate = new ConcurrentTimerSet<IComputed>(
                new ConcurrentTimerSet<IComputed>.Options() {
                    Quanta = TimeSpan.FromMilliseconds(250),
                    ConcurrencyLevel = concurrencyLevel,
                    Clock = Clock,
                },
                t => t.Invalidate());
        }
    }
}
