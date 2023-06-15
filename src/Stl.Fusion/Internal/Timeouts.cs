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
        var keepAliveQuanta = TimeSpan.FromMilliseconds(250);
        var invalidateQuanta = TimeSpan.FromMilliseconds(250);
        KeepAlive = new ConcurrentTimerSet<object>(
            new() {
                Quanta = keepAliveQuanta,
                ConcurrencyLevel = HardwareInfo.GetProcessorCountPo2Factor(),
                Clock = Clock,
            }, null, Clock.Now - keepAliveQuanta.Multiply(2)); // Start in past makes timer priorities strictly positive
        Invalidate = new ConcurrentTimerSet<IComputed>(
            new() {
                Quanta = invalidateQuanta,
                ConcurrencyLevel = HardwareInfo.GetProcessorCountPo2Factor(),
                Clock = Clock,
            },
            t => t.Invalidate(true));
    }
}
