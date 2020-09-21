using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Authentication
{
    public interface IAuthService
    {
        Task LogoutAsync(AuthSession? session = null, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<AuthUser> GetUserAsync(AuthSession? session = null, CancellationToken cancellationToken = default);
    }

    public interface IServerAuthService : IAuthService
    {
        Task LoginAsync(AuthUser user, AuthSession? session = null, CancellationToken cancellationToken = default);
    }
}
