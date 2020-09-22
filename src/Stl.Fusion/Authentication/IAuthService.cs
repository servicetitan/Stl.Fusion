using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Authentication
{
    public interface IAuthService
    {
        Task LogoutAsync(AuthContext? context = null, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<AuthUser> GetUserAsync(AuthContext? context = null, CancellationToken cancellationToken = default);
    }

    public interface IServerSideAuthService : IAuthService
    {
        Task LoginAsync(AuthUser user, AuthContext? context = null, CancellationToken cancellationToken = default);
    }
}
