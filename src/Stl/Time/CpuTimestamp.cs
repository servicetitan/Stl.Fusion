using System.Diagnostics;
using Cysharp.Text;

namespace Stl.Time;

public readonly record struct CpuTimestamp(long Value) : IComparable<CpuTimestamp>
{
    private static readonly Func<long> QueryPerformanceCounter;

    public static readonly long TickFrequency;
    public static readonly double TickDuration;

    static CpuTimestamp()
    {
        var mQueryPerformanceCounter = typeof(Stopwatch)
            .GetMethod(
                nameof(QueryPerformanceCounter),
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.NonPublic);
        if (mQueryPerformanceCounter != null) {
            // .NET + .NET Core, WASM
            TickFrequency = Stopwatch.Frequency;
            QueryPerformanceCounter = (Func<long>)mQueryPerformanceCounter!
                .CreateDelegate(typeof(Func<long>));
        }
        else {
            // .NET Framework 
            TickFrequency = 10_000_000;
            QueryPerformanceCounter = Stopwatch.GetTimestamp;
        }
        TickDuration = 1d / TickFrequency;
    }

    public TimeSpan Elapsed {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Now - this;
    }

    public static CpuTimestamp Now {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(QueryPerformanceCounter.Invoke());
    }

    public override string ToString()
        => ZString.Concat(Elapsed.ToShortString(), " elapsed");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan operator -(CpuTimestamp a, CpuTimestamp b)
        => TimeSpan.FromSeconds(TickDuration * (a.Value - b.Value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(CpuTimestamp other)
        => Value.CompareTo(other.Value);
}
