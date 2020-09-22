using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Authentication;
using Stl.Fusion.Client.RestEase;

namespace Stl.Fusion.Client.Authentication
{
    [BasePath("fusion/auth")]
    public interface IAuthClient : IRestEaseReplicaClient
    {
        [Get("logout")]
        Task LogoutAsync(AuthContext? context = null, CancellationToken cancellationToken = default);
        [Get("getUser"), ComputeMethod]
        Task<AuthUser> GetUserAsync(AuthContext? context = null, CancellationToken cancellationToken = default);
    }
}
