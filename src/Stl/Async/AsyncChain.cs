namespace Stl.Async;

[StructLayout(LayoutKind.Auto)]
public readonly record struct AsyncChain(
    string Name,
    Func<CancellationToken, Task> Start,
    TerminalErrorDetector TerminalErrorDetector)
{
    public static readonly AsyncChain None = new("(no-operation)", _ => Task.CompletedTask);
    public static readonly AsyncChain NeverEnding = new("(never-ending)", _ => TaskExt.NeverEndingTask);

    public AsyncChain(string name, Func<CancellationToken, Task> start)
        : this(name, start, TerminalError.Detector) { }
    public AsyncChain(Func<CancellationToken, Task> start)
        : this("(unnamed)", start, TerminalError.Detector) { }

    // Constructor-like methods

    public static AsyncChain Delay(TimeSpan timeSpan, IMomentClock? clock = null)
        => new($"Delay({timeSpan.ToString()})",
            ct => (clock ?? MomentClockSet.Default.CpuClock).Delay(timeSpan, ct));

    public static AsyncChain Delay(RandomTimeSpan delay, IMomentClock? clock = null)
        => new($"Delay({delay.ToString()})",
            ct => (clock ?? MomentClockSet.Default.CpuClock).Delay(delay.Next(), ct));

    // Conversion

    public override string ToString() => Name;

    public static implicit operator AsyncChain(Func<CancellationToken, Task> start) => new(start);
    public static implicit operator AsyncChain(RandomTimeSpan value) => Delay(value);
    public static implicit operator AsyncChain(TimeSpan value) => Delay(value);

    // Operators

    public static AsyncChain operator &(AsyncChain first, AsyncChain second) => first.Append(second);

    // Other methods

    public Task Run(CancellationToken cancellationToken = default)
    {
        var start = Start;
        return Task.Run(() => start(cancellationToken), cancellationToken);
    }

    public Task RunIsolated(CancellationToken cancellationToken = default)
    {
        using var _ = ExecutionContextExt.SuppressFlow();
        return Run(cancellationToken);
    }
}
