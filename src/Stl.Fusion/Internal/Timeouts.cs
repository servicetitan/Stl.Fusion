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
        public static readonly IMomentClock Clock;

        static Timeouts()
        {
            Clock = CoarseCpuClock.Instance;
            var concurrencyLevel = HardwareInfo.ProcessorCountPo2 << 4;
            KeepAlive = new ConcurrentTimerSet<object>(
                new ConcurrentTimerSet<object>.Options() {
                    Quanta = TimeSpan.FromMilliseconds(250),
                    ConcurrencyLevel = concurrencyLevel,
                    Clock = Clock,
                });
            Swap = new ConcurrentTimerSet<ISwappable>(
                new ConcurrentTimerSet<ISwappable>.Options() {
                    Quanta = TimeSpan.FromSeconds(1),
                    ConcurrencyLevel = concurrencyLevel,
                    Clock = Clock,
                    FireHandler = t => t.SwapAsync(),
                });
        }
    }
}
