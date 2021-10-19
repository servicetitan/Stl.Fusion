namespace Stl.Fusion.Internal;

public sealed class FuncComputedState<T> : ComputedState<T>
{
    public Func<IComputedState<T>, CancellationToken, Task<T>> Computer { get; }

    public FuncComputedState(
        Options options,
        IServiceProvider services,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer)
        : base(options, services, false)
    {
        Computer = computer;
        Initialize(options);
    }

    protected override Task<T> Compute(CancellationToken cancellationToken)
        => Computer.Invoke(this, cancellationToken);
}
