namespace Stl.Time
{
    public class MomentClockSet
    {
        public static MomentClockSet Default { get; set; } = new(Stl.Time.SystemClock.Instance) {
            CpuClock = Stl.Time.CpuClock.Instance,
            CoarseCpuClock = Stl.Time.CoarseCpuClock.Instance,
            UIClock = Stl.Time.CpuClock.Instance,
        };

        public IMomentClock SystemClock { get; init; }
        public IMomentClock CpuClock { get; init; }
        public IMomentClock CoarseCpuClock { get; init; }
        public IMomentClock UIClock { get; init; }

        public MomentClockSet()
        {
            SystemClock = Default.SystemClock;
            CpuClock = Default.CpuClock;
            CoarseCpuClock = Default.CoarseCpuClock;
            UIClock = Default.UIClock;
        }

        public MomentClockSet(IMomentClock anyClock)
        {
            SystemClock = anyClock;
            CpuClock = anyClock;
            CoarseCpuClock = anyClock;
            UIClock = anyClock;
        }
    }
}
