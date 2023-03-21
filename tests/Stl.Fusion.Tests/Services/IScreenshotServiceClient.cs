using RestEase;
using Stl.Fusion.Client;
using Stl.Interception;

namespace Stl.Fusion.Tests.Services;

[RegisterRestEaseReplicaService(Scope = ServiceScope.ClientServices)]
[BasePath("screenshot")]
public interface IScreenshotServiceClient : IRequiresAsyncProxy
{
    [Get(nameof(GetScreenshot)), ComputeMethod(MinCacheDuration = 1)]
    Task<Screenshot> GetScreenshot(int width, CancellationToken cancellationToken = default);
}
