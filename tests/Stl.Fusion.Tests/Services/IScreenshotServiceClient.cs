using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Client;

namespace Stl.Fusion.Tests.Services
{
    [RestEaseReplicaService(Scope = ServiceScope.ClientServices)]
    [BasePath("screenshot")]
    public interface IScreenshotServiceClient
    {
        [Get("getScreenshot"), ComputeMethod(KeepAliveTime = 1)]
        Task<Screenshot> GetScreenshotAsync(int width, CancellationToken cancellationToken = default);
    }
}
