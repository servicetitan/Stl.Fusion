using System.Diagnostics;
using System.Reactive.PlatformServices;

namespace Stl.Time;

public sealed class CpuClock : IMomentClock
{
    private static readonly DateTime Zero = DateTime.UtcNow;
    private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();
    public static readonly IMomentClock Instance = new CpuClock();

    public static Moment Now {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Zero + Stopwatch.Elapsed).ToMoment();
    }

    Moment IMomentClock.Now => Now;
    DateTimeOffset ISystemClock.UtcNow => Now;

    private CpuClock() { }

    public override string ToString() => $"{GetType().Name}()";
    public Moment ToRealTime(Moment localTime) => localTime;
    public Moment ToLocalTime(Moment realTime) => realTime;
    public TimeSpan ToRealDuration(TimeSpan localDuration) => localDuration;
    public TimeSpan ToLocalDuration(TimeSpan realDuration) => realDuration;

    public Task Delay(TimeSpan dueIn, CancellationToken cancellationToken = default)
        => Task.Delay(dueIn, cancellationToken);
}
