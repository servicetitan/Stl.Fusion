using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Extensions.Internal
{
    public interface IKeyValueStoreSandboxProvider
    {
        Task<KeyValueStoreSandbox> GetSandbox(Session session, CancellationToken cancellationToken = default);
    }
}
