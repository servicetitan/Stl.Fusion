using System.Diagnostics;

namespace Stl.Time;

public record struct CpuTimestamp(long Value) : IComparable<CpuTimestamp>
{
    public static readonly long TicksPerSecond = 10_000_000;
    public static readonly double SecondsPerTick;

    static CpuTimestamp()
    {
        var tickFrequency = typeof(Stopwatch)
            .GetField("s_tickFrequency",
                BindingFlags.Static | BindingFlags.NonPublic)
            ?.GetValue(null);
        if (tickFrequency is double tf)
            TicksPerSecond = (long)tf * Stopwatch.Frequency;
        SecondsPerTick = 1d / TicksPerSecond;
    }

    public static CpuTimestamp Now {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Stopwatch.GetTimestamp());
    }

    public override string ToString()
        => Value + " ticks";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan Elapsed(CpuTimestamp from)
        => Now - from;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan operator -(CpuTimestamp a, CpuTimestamp b)
        => TimeSpan.FromSeconds(SecondsPerTick * (a.Value - b.Value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(CpuTimestamp other)
        => Value.CompareTo(other.Value);
}
