using System.Reactive.PlatformServices;

namespace Stl.Time;

public interface IServerClock : IMomentClock
{
    Task WhenReady { get; }
}

public class ServerClock : IServerClock
{
    private volatile Task<TimeSpan> _offsetTask = TaskSource.New<TimeSpan>(true).Task;

    public IMomentClock BaseClock { get; }

    public TimeSpan Offset {
        get => _offsetTask.IsCompleted ? _offsetTask.Result : default;
        set {
            if (_offsetTask.IsCompleted)
                _offsetTask = Task.FromResult(value);
            else
                TaskSource.For(_offsetTask).TrySetResult(value);
        }
    }

    public Moment Now {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => BaseClock.Now + Offset;
    }

    Moment IMomentClock.Now => Now;
    DateTimeOffset ISystemClock.UtcNow => Now;
    public Task WhenReady => _offsetTask;

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
