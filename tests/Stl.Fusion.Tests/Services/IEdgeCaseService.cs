using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Tests.Services
{
    public interface IEdgeCaseService
    {
        Task<string> GetSuffixAsync(CancellationToken cancellationToken = default);
        Task SetSuffixAsync(string suffix, CancellationToken cancellationToken = default);

        [ComputeMethod(KeepAliveTime = 10)]
        Task<string> ThrowIfContainsErrorAsync(string source, CancellationToken cancellationToken = default);
        [ComputeMethod(RewriteErrors = true, KeepAliveTime = 10)]
        Task<string> ThrowIfContainsErrorRewriteErrorsAsync(string source, CancellationToken cancellationToken = default);
        Task<string> ThrowIfContainsErrorNonComputeAsync(string source, CancellationToken cancellationToken = default);
    }
}
