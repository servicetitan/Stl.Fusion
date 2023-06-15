using Stl.Fusion.Interception;

namespace Stl.Fusion.Client.Cache;

public sealed class NoClientComputedCache : ClientComputedCache
{
    public NoClientComputedCache(IServiceProvider services) : base(services) { }

    protected override ValueTask<Result<T>?> GetInternal<T>(ComputeMethodInput input, CancellationToken cancellationToken)
        => ValueTaskExt.FromResult((Result<T>?)null);

    protected override ValueTask SetInternal<T>(ComputeMethodInput input, Result<T>? output, CancellationToken cancellationToken)
        => ValueTaskExt.CompletedTask;
}