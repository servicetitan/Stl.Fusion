using Stl.Fusion.Authentication;

namespace Stl.Fusion.Extensions;

public interface IBackendStatus
{
    public ImmutableArray<string> Backends { get; }

    [ComputeMethod]
    public Task<ImmutableList<(string Backend, Exception Error)>> GetAllErrors(
        Session session,
        CancellationToken cancellationToken = default);

    [ComputeMethod]
    public Task<Exception?> GetError(
        Session session,
        string backend,
        CancellationToken cancellationToken = default);
}
