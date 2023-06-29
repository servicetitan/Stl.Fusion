namespace Stl.Fusion.Internal;

public static class Timeouts
{
    public static readonly IMomentClock Clock;
    public static readonly ConcurrentTimerSet<object> KeepAlive;
    public static readonly ConcurrentTimerSet<IComputed> Invalidate;
    public static readonly Moment StartedAt;
    public const int KeepAliveQuantaPo2 = 21; // ~ 2M ticks or 0.2 sec.
    public static readonly TimeSpan KeepAliveQuanta = TimeSpan.FromTicks(1L << KeepAliveQuantaPo2);

    static Timeouts()
    {
        Clock = MomentClockSet.Default.CpuClock;
        StartedAt = Clock.Now - KeepAliveQuanta.Multiply(2); // In past to make timer priorities strictly positive
        KeepAlive = new ConcurrentTimerSet<object>(
            new() {
                Quanta = KeepAliveQuanta,
                ConcurrencyLevel = FusionSettings.TimeoutsConcurrencyLevel,
                Clock = Clock,
            }, null, StartedAt);
        Invalidate = new ConcurrentTimerSet<IComputed>(
            new() {
                Quanta = KeepAliveQuanta,
                ConcurrencyLevel = FusionSettings.TimeoutsConcurrencyLevel,
                Clock = Clock,
            },
            t => t.Invalidate(true), StartedAt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetKeepAliveSlot(Moment moment)
        => (moment.EpochOffsetTicks - StartedAt.EpochOffsetTicks) >> KeepAliveQuantaPo2;
}
