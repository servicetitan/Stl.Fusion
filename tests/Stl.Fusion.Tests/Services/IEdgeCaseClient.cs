using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Client;
using Stl.Fusion.Client.RestEase;

namespace Stl.Fusion.Tests.Services
{
    [RestEaseReplicaService]
    [BasePath("EdgeCase")]
    public interface IEdgeCaseClient : IRestEaseReplicaClient
    {
        [Get("GetSuffix")]
        Task<string> GetSuffixAsync(CancellationToken cancellationToken = default);
        [Post("SetSuffix")]
        Task SetSuffixAsync(string suffix, CancellationToken cancellationToken = default);

        [Get("ThrowIfContainsError"), ComputeMethod]
        Task<string> ThrowIfContainsErrorAsync(string source, CancellationToken cancellationToken = default);
        [Get("ThrowIfContainsErrorRewriteErrors"), ComputeMethod]
        Task<string> ThrowIfContainsErrorRewriteErrorsAsync(string source, CancellationToken cancellationToken = default);
        [Get("ThrowIfContainsErrorNonCompute")]
        Task<string> ThrowIfContainsErrorNonComputeAsync(string source, CancellationToken cancellationToken = default);
    }

    [RestEaseReplicaService]
    [BasePath("EdgeCaseRewrite")]
    public interface IEdgeCaseRewriteClient : IEdgeCaseClient { }
}
