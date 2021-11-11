using Stl.Fusion.Authentication;

namespace Stl.Fusion.Extensions;

public interface IBackendStatus
{
    [ComputeMethod(KeepAliveTime = 10)]
    public Task<Exception?> GetError(
        Session session,
        string? backendName = null,
        CancellationToken cancellationToken = default);
}
