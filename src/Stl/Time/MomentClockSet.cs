namespace Stl.Time;

public class MomentClockSet
{
    public static MomentClockSet Default { get; set; } = new(
        Stl.Time.SystemClock.Instance,
        Stl.Time.CpuClock.Instance,
        new ServerClock(Stl.Time.CpuClock.Instance),
        Stl.Time.CoarseSystemClock.Instance,
        Stl.Time.CoarseCpuClock.Instance);

    public IMomentClock SystemClock { get; init; }
    public IMomentClock CpuClock { get; init; }
    public IServerClock ServerClock { get; init; }
    public IMomentClock CoarseSystemClock { get; init; }
    public IMomentClock CoarseCpuClock { get; init; }

    public MomentClockSet()
    {
        SystemClock = Default.SystemClock;
        CpuClock = Default.CpuClock;
        ServerClock = Default.ServerClock;
        CoarseSystemClock = Default.CoarseSystemClock;
        CoarseCpuClock = Default.CoarseCpuClock;
    }

    public MomentClockSet(
        IMomentClock systemClock,
        IMomentClock cpuClock,
        IServerClock serverClock,
        IMomentClock coarseSystemClock,
        IMomentClock coarseCpuClock)
    {
        SystemClock = systemClock;
        CpuClock = cpuClock;
        ServerClock = serverClock;
        CoarseSystemClock = coarseSystemClock;
        CoarseCpuClock = coarseCpuClock;
    }
}
