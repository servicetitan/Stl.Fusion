using Stl.Fusion.Swapping;
using Stl.OS;

namespace Stl.Fusion.Internal;

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
            new() {
                Quanta = TimeSpan.FromMilliseconds(250),
                ConcurrencyLevel = concurrencyLevel,
                Clock = Clock,
            });
        Swap = new ConcurrentTimerSet<ISwappable>(
            new() {
                Quanta = TimeSpan.FromMilliseconds(250),
                ConcurrencyLevel = concurrencyLevel,
                Clock = Clock,
            },
            t => t.Swap());
        Invalidate = new ConcurrentTimerSet<IComputed>(
            new() {
                Quanta = TimeSpan.FromMilliseconds(250),
                ConcurrencyLevel = concurrencyLevel,
                Clock = Clock,
            },
            t => t.Invalidate());
    }
}
