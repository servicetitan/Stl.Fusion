namespace Stl.Fusion.Blazor.Internal;

public sealed class ComputedStateComponentState<T>(
    ComputedState<T>.Options settings,
    Func<IComputedState<T>, CancellationToken, Task<T>> computer,
    IServiceProvider services)
    : ComputedState<T>(settings, services, false), IHasInitialize
{
    public readonly Options Settings = settings;
    public readonly Func<IComputedState<T>, CancellationToken, Task<T>> Computer = computer;

    void IHasInitialize.Initialize(object? settings)
        => base.Initialize(Settings);

    protected override Task<T> Compute(CancellationToken cancellationToken)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(ToString());

        return Computer.Invoke(this, cancellationToken);
    }
}
