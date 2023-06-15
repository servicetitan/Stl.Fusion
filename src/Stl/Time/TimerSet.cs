namespace Stl.Time;

public record TimerSetOptions
{
    // ReSharper disable once StaticMemberInGenericType
    public static TimeSpan MinQuanta { get; } = TimeSpan.FromMilliseconds(10);

    public TimeSpan Quanta { get; init; } = TimeSpan.FromSeconds(1);
    public IMomentClock Clock { get; init; } = MomentClockSet.Default.CpuClock;
}

public sealed class TimerSet<TTimer> : WorkerBase
    where TTimer : notnull
{
    private readonly Action<TTimer>? _fireHandler;
    private readonly RadixHeapSet<TTimer> _timers = new(45);
    private readonly Moment _start;
    private readonly object _lock = new();
    private int _minPriority = 0;

    public TimeSpan Quanta { get; }
    public IMomentClock Clock { get; }
    public int Count {
        get {
            lock (_lock) return _timers.Count;
        }
    }

    public TimerSet(TimerSetOptions options, Action<TTimer>? fireHandler = null, Moment? start = null)
    {
        if (options.Quanta < TimerSetOptions.MinQuanta)
            throw new ArgumentOutOfRangeException(nameof(options), "Quanta < MinQuanta.");

        Quanta = options.Quanta;
        Clock = options.Clock;
        _fireHandler = fireHandler;
        _start = start ?? Clock.Now;
        _ = Run();
    }

    public long GetPriority(Moment time)
        => (time - _start).Ticks / Quanta.Ticks;

    public void AddOrUpdate(TTimer timer, Moment time)
        => AddOrUpdate(timer, GetPriority(time));
    public void AddOrUpdate(TTimer timer, long priority)
    {
        lock (_lock)
            _timers.AddOrUpdate(FixPriority(priority), timer);
    }

    public bool AddOrUpdateToEarlier(TTimer timer, Moment time)
        => AddOrUpdateToEarlier(timer, GetPriority(time));
    public bool AddOrUpdateToEarlier(TTimer timer, long priority)
    {
        lock (_lock) {
            return _timers.AddOrUpdateToLower(FixPriority(priority), timer);
        }
    }

    public bool AddOrUpdateToLater(TTimer timer, Moment time)
        => AddOrUpdateToLater(timer, GetPriority(time));
    public bool AddOrUpdateToLater(TTimer timer, long priority)
    {
        lock (_lock) {
            return _timers.AddOrUpdateToHigher(FixPriority(priority), timer);
        }
    }

    public bool Remove(TTimer timer)
    {
        lock (_lock)
            return _timers.Remove(timer, out _);
    }

    // Protected & private methods

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        var dueAt = _start + Quanta;
        for (;; dueAt += Quanta) {
            cancellationToken.ThrowIfCancellationRequested();
            if (dueAt > Clock.Now)
                // We intentionally don't pass CancellationToken here:
                // the delay is supposed to be short & we want to save on
                // CancellationToken registration/de-registration.
                await Clock.Delay(dueAt, CancellationToken.None).ConfigureAwait(false);
            IReadOnlyDictionary<TTimer, long> minSet;
            lock (_lock) {
                minSet = _timers.ExtractMinSet(_minPriority);
                ++_minPriority;
            }
            if (_fireHandler != null && minSet.Count != 0) {
                foreach (var (timer, _) in minSet) {
                    try {
                        _fireHandler(timer);
                    }
                    catch {
                        // Intended suppression
                    }
                }
            }
        }
    }

    // Private methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long FixPriority(long priority)
        => Math.Max(_minPriority, priority);
}
