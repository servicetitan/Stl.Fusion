using Stl.OS;

namespace Stl.Time;

public readonly struct DelayedAction : IEquatable<DelayedAction>
{
    public static readonly IMomentClock Clock;
    public static readonly ConcurrentTimerSet<DelayedAction> Instances;

    public object Target { get; }
    public Action<object>? Handler { get; }

    static DelayedAction()
    {
        Clock = MomentClockSet.Default.CpuClock;
        Instances = new ConcurrentTimerSet<DelayedAction>(
            new() {
                Quanta = TimeSpan.FromMilliseconds(1000),
                ConcurrencyLevel = Math.Max(1, HardwareInfo.ProcessorCountPo2 >> 8), // Supposed to be rarely used
                Clock = Clock,
            },
            t => t.Invoke());
    }

    public DelayedAction(object target, Action<object>? handler)
    {
        Target = target;
        Handler = handler;
    }

    public void Invoke()
        => Handler?.Invoke(Target);

    // Equality - uses only Target property

    public bool Equals(DelayedAction other) => Equals(Target, other.Target);
    public override bool Equals(object? obj) => obj is DelayedAction other && Equals(other);
    public override int GetHashCode() => Target.GetHashCode();
    public static bool operator ==(DelayedAction left, DelayedAction right) => left.Equals(right);
    public static bool operator !=(DelayedAction left, DelayedAction right) => !left.Equals(right);
}
