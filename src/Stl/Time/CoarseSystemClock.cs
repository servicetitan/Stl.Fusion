using System.Reactive.PlatformServices;
using Stl.Time.Internal;

namespace Stl.Time;

public sealed class CoarseSystemClock : IMomentClock
{
    public static readonly IMomentClock Instance = new CoarseSystemClock();

    public static Moment Now {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => CoarseClockHelper.SystemNow;
    }

    Moment IMomentClock.Now => Now;
    DateTimeOffset ISystemClock.UtcNow => Now;

    private CoarseSystemClock() { }

    public override string ToString() => $"{GetType().Name}()";
    public Moment ToRealTime(Moment localTime) => localTime;
    public Moment ToLocalTime(Moment realTime) => realTime;
    public TimeSpan ToRealDuration(TimeSpan localDuration) => localDuration;
    public TimeSpan ToLocalDuration(TimeSpan realDuration) => realDuration;

    public Task Delay(TimeSpan dueIn, CancellationToken cancellationToken = default)
        => Task.Delay(dueIn, cancellationToken);
}
