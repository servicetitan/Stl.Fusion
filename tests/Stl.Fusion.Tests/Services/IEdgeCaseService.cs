namespace Stl.Fusion.Tests.Services;

public interface IEdgeCaseService
{
    [ComputeMethod(MinCacheDuration = 10)]
    Task<string> GetSuffix(CancellationToken cancellationToken = default);
    Task SetSuffix(string suffix, CancellationToken cancellationToken = default);

    [ComputeMethod(MinCacheDuration = 10)]
    Task<long?> GetNullable(long source, CancellationToken cancellationToken = default);

    [ComputeMethod(MinCacheDuration = 10)]
    Task<string> ThrowIfContainsError(string source, CancellationToken cancellationToken = default);
    Task<string> ThrowIfContainsErrorNonCompute(string source, CancellationToken cancellationToken = default);
}
