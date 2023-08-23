namespace Stl.Time;

public class MomentClockSet(IMomentClock systemClock,
    IMomentClock cpuClock,
    IServerClock serverClock,
    IMomentClock coarseSystemClock,
    IMomentClock coarseCpuClock)
{
    public static MomentClockSet Default { get; set; } = new(
        Stl.Time.SystemClock.Instance,
        Stl.Time.CpuClock.Instance,
        new ServerClock(Stl.Time.CpuClock.Instance),
        Stl.Time.CoarseSystemClock.Instance,
        Stl.Time.CoarseCpuClock.Instance);

    public IMomentClock SystemClock { get; init; } = systemClock;
    public IMomentClock CpuClock { get; init; } = cpuClock;
    public IServerClock ServerClock { get; init; } = serverClock;
    public IMomentClock CoarseSystemClock { get; init; } = coarseSystemClock;
    public IMomentClock CoarseCpuClock { get; init; } = coarseCpuClock;

    public MomentClockSet() : this(
        Default.SystemClock,
        Default.CpuClock,
        Default.ServerClock,
        Default.CoarseSystemClock,
        Default.CoarseCpuClock)
    { }

    public MomentClockSet(IMomentClock anyClock)
        : this(anyClock, anyClock, new ServerClock(anyClock), anyClock, anyClock)
    { }
}
