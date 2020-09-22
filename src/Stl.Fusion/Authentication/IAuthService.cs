using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Authentication
{
    public interface IAuthService
    {
        Task LogoutAsync(Session? session = null, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<User> GetUserAsync(Session? session = null, CancellationToken cancellationToken = default);
    }

    public interface IServerSideAuthService : IAuthService
    {
        Task LoginAsync(User user, Session? session = null, CancellationToken cancellationToken = default);
    }
}
