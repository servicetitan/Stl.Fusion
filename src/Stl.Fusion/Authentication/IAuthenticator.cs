using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Authentication
{
    public interface IAuthenticator
    {
        Task LogoutAsync(Session? session = null, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<Principal> GetUserAsync(Session? session = null, CancellationToken cancellationToken = default);
    }

    public interface IServerAuthenticator : IAuthenticator
    {
        Task LoginAsync(Principal user, Session? session = null, CancellationToken cancellationToken = default);
    }
}
