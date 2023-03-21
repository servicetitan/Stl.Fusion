namespace Stl.Fusion.Tests.Services;

public class MathService : IComputeService
{
    [ComputeMethod]
    public virtual Task<int> Sum(int[]? values, CancellationToken cancellationToken = default)
        => Task.FromResult(values?.Sum() ?? 0);
}
