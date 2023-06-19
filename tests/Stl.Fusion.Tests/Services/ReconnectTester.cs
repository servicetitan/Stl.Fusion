namespace Stl.Fusion.Tests.Services;

public interface IReconnectTester : IComputeService
{
    [ComputeMethod]
    Task<(int, int)> Delay(int delay, int invalidationDelay, CancellationToken cancellationToken = default);
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
}
