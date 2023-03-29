using Stl.OS;

namespace Stl.Fusion.Internal;

public static class Timeouts
{
    public static readonly IMomentClock Clock;
    public static readonly ConcurrentTimerSet<object> KeepAlive;
    public static readonly ConcurrentTimerSet<IComputed> Invalidate;

    static Timeouts()
    {
        Clock = MomentClockSet.Default.CpuClock;
        KeepAlive = new ConcurrentTimerSet<object>(
            new() {
                Quanta = TimeSpan.FromMilliseconds(250),
                ConcurrencyLevel = HardwareInfo.GetProcessorCountPo2Factor(2),
                Clock = Clock,
            });
        Invalidate = new ConcurrentTimerSet<IComputed>(
            new() {
                Quanta = TimeSpan.FromMilliseconds(250),
                ConcurrencyLevel = HardwareInfo.GetProcessorCountPo2Factor(),
                Clock = Clock,
            },
            t => t.Invalidate(true));
    }
}
