namespace Stl.Fusion.Tests.Services;

public interface IReconnectTester : IComputeService
{
    [ComputeMethod]
    Task<(int, int)> Delay(int delay, int invalidationDelay, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<Moment> GetTime(CancellationToken cancellationToken = default);
}

public class ReconnectTester : IReconnectTester
{
    public virtual async Task<(int, int)> Delay(int delay, int invalidationDelay, CancellationToken cancellationToken = default)
    {
        var computed = Computed.GetCurrent();
        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        _ = Task.Run(async () => {
            await Task.Delay(invalidationDelay, CancellationToken.None).ConfigureAwait(false);
            computed!.Invalidate();
        });
        return (delay, invalidationDelay);
    }

    public virtual Task<Moment> GetTime(CancellationToken cancellationToken = default)
        => Task.FromResult(SystemClock.Now);

    public void InvalidateGetTime()
    {
        using var scope = Computed.Invalidate();
        _ = GetTime();
    }
}
