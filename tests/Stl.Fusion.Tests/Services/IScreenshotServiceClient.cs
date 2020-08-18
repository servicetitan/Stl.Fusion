using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Client;
using Stl.Fusion.Client.RestEase;

namespace Stl.Fusion.Tests.Services
{
    [RestEaseReplicaService]
    [BasePath("screenshot")]
    public interface IScreenshotServiceClient : IRestEaseReplicaClient
    {
        [Get("getScreenshot"), ComputeMethod(KeepAliveTime = 0.3)]
        Task<Screenshot> GetScreenshotAsync(int width, CancellationToken cancellationToken = default);
    }
}
