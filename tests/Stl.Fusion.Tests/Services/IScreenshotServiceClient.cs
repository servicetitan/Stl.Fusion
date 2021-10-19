using RestEase;
using Stl.Fusion.Client;

namespace Stl.Fusion.Tests.Services;

[RegisterRestEaseReplicaService(Scope = ServiceScope.ClientServices)]
[BasePath("screenshot")]
public interface IScreenshotServiceClient
{
    [Get("getScreenshot"), ComputeMethod(KeepAliveTime = 1)]
    Task<Screenshot> GetScreenshot(int width, CancellationToken cancellationToken = default);
}
