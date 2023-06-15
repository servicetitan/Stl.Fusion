using Stl.OS;

namespace Stl.Time;

public record ConcurrentTimerSetOptions : TimerSetOptions
{
    public static new readonly ConcurrentTimerSetOptions Default = new();

    public int ConcurrencyLevel { get; init; } = HardwareInfo.GetProcessorCountPo2Factor();
}

public sealed class ConcurrentTimerSet<TTimer> : SafeAsyncDisposableBase
    where TTimer : notnull
{
    private readonly TimerSet<TTimer>[] _timerSets;
    private readonly int _concurrencyLevelMask;
    private readonly Moment _start;

    public TimeSpan Quanta { get; }
    public IMomentClock Clock { get; }
    public int ConcurrencyLevel { get; }
    public int Count => _timerSets.Sum(ts => ts.Count);

    public ConcurrentTimerSet(ConcurrentTimerSetOptions options, Action<TTimer>? fireHandler = null, Moment? start = null)
    {
        if (options.Quanta < TimerSetOptions.MinQuanta)
            throw new ArgumentOutOfRangeException(nameof(options), "Quanta < MinQuanta.");

        Quanta = options.Quanta;
        Clock = options.Clock;
        ConcurrencyLevel = (int) Bits.GreaterOrEqualPowerOf2((ulong) Math.Max(1, options.ConcurrencyLevel));
        _concurrencyLevelMask = ConcurrencyLevel - 1;
        _start = start ?? Clock.Now;
        _timerSets = new TimerSet<TTimer>[ConcurrencyLevel];
        for (var i = 0; i < _timerSets.Length; i++)
            _timerSets[i] = new TimerSet<TTimer>(options, fireHandler, _start);
    }

    protected override Task DisposeAsync(bool disposing)
    {
        if (!disposing) return Task.CompletedTask;

        var tasks = new List<Task>(_timerSets.Length);
        foreach (var timerSet in _timerSets)
            tasks.Add(timerSet.DisposeAsync().AsTask());
        return Task.WhenAll(tasks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetPriority(Moment time)
        => (time - _start).Ticks / Quanta.Ticks;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddOrUpdate(TTimer timer, Moment time)
        => GetTimerSet(timer).AddOrUpdate(timer, time);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddOrUpdate(TTimer timer, long priority)
        => GetTimerSet(timer).AddOrUpdate(timer, priority);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AddOrUpdateToEarlier(TTimer timer, Moment time)
        => GetTimerSet(timer).AddOrUpdateToEarlier(timer, time);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AddOrUpdateToEarlier(TTimer timer, long priority)
        => GetTimerSet(timer).AddOrUpdateToEarlier(timer, priority);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AddOrUpdateToLater(TTimer timer, Moment time)
        => GetTimerSet(timer).AddOrUpdateToLater(timer, time);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AddOrUpdateToLater(TTimer timer, long priority)
        => GetTimerSet(timer).AddOrUpdateToLater(timer, priority);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TTimer timer)
        => GetTimerSet(timer).Remove(timer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TimerSet<TTimer> GetTimerSet(TTimer timer)
        => _timerSets[timer.GetHashCode() & _concurrencyLevelMask];
}
