using System.Reactive.PlatformServices;

namespace Stl.Time;

public interface IServerClock : IMomentClock
{
    IMomentClock BaseClock { get; }
    TimeSpan Offset { get; set; }
    Task WhenReady { get; }
}

public class ServerClock : IServerClock
{
    private volatile TaskCompletionSource<TimeSpan> _offsetSource = TaskCompletionSourceExt.New<TimeSpan>();

    public IMomentClock BaseClock { get; }

    public TimeSpan Offset {
        get {
            var offsetTask = _offsetSource.Task;
            return offsetTask.IsCompleted ? offsetTask.Result : default;
        }
        set {
            if (_offsetSource.Task.IsCompleted)
                _offsetSource = TaskCompletionSourceExt.New<TimeSpan>().WithResult(value);
            else
                _offsetSource.TrySetResult(value);
        }
    }

    public Moment Now {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => BaseClock.Now + Offset;
    }

    Moment IMomentClock.Now => Now;
    DateTimeOffset ISystemClock.UtcNow => Now;
    public Task WhenReady => _offsetSource.Task;

    public ServerClock(IMomentClock? baseClock = null)
        => BaseClock = baseClock ?? MomentClockSet.Default.CpuClock;

    public override string ToString() => $"{GetType().Name}()";
    public Moment ToRealTime(Moment localTime) => localTime;
    public Moment ToLocalTime(Moment realTime) => realTime;
    public TimeSpan ToRealDuration(TimeSpan localDuration) => localDuration;
    public TimeSpan ToLocalDuration(TimeSpan realDuration) => realDuration;

    public Task Delay(TimeSpan dueIn, CancellationToken cancellationToken = default)
        // TODO: Make it work properly, i.e. taking into account time changes, sleep/resume, etc.
        => Task.Delay(dueIn, cancellationToken);
}
