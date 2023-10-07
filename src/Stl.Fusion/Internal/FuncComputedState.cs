namespace Stl.Fusion.Internal;

public sealed class FuncComputedState<T> : ComputedState<T>
{
    public Func<IComputedState<T>, CancellationToken, Task<T>> Computer { get; }

    public FuncComputedState(
        Options settings,
        IServiceProvider services,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer)
        : base(settings, services, false)
    {
        Computer = computer;
        Initialize(settings);
    }

    protected override Task<T> Compute(CancellationToken cancellationToken)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(ToString());

        return Computer.Invoke(this, cancellationToken);
    }
}
