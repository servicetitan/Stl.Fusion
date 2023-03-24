using RestEase;
using Stl.Fusion.Client;

namespace Stl.Fusion.Tests.Services;

[RegisterRestEaseReplicaService(Scope = ServiceScope.ClientServices)]
[BasePath("EdgeCase")]
public interface IEdgeCaseClient : IComputeService
{
    [ComputeMethod(MinCacheDuration = 10)]
    [Get(nameof(GetSuffix))]
    Task<string> GetSuffix(CancellationToken cancellationToken = default);
    [Post(nameof(SetSuffix))]
    Task SetSuffix(string suffix, CancellationToken cancellationToken = default);

    [ComputeMethod(MinCacheDuration = 10)]
    [Get(nameof(GetNullable))]
    Task<long?> GetNullable(long source, CancellationToken cancellationToken = default);

    [ComputeMethod(MinCacheDuration = 10)]
    [Get(nameof(ThrowIfContainsError))]
    Task<string> ThrowIfContainsError(string source, CancellationToken cancellationToken = default);
    [Get(nameof(ThrowIfContainsErrorNonCompute))]
    Task<string> ThrowIfContainsErrorNonCompute(string source, CancellationToken cancellationToken = default);
}
