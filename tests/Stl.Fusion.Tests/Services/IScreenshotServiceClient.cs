using RestEase;
using Stl.Fusion.Client;

namespace Stl.Fusion.Tests.Services;

[RegisterRestEaseReplicaService(Scope = ServiceScope.ClientServices)]
[BasePath("screenshot")]
public interface IScreenshotServiceClient : IComputeService
{
    [Get(nameof(GetScreenshot)), ComputeMethod(MinCacheDuration = 1)]
    Task<Screenshot> GetScreenshot(int width, CancellationToken cancellationToken = default);
}
