using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;

namespace HelloClientServerFx
{
    // Ideally, we want Replica Service to be exactly the same as corresponding
    // Compute Service. A good way to enforce this is to expose an interface
    // that should be implemented by Compute Service + tell Fusion to "expose"
    // the client via the same interface.
    public interface ICounterService
    {
        [ComputeMethod]
        Task<int> Get(string key, CancellationToken cancellationToken = default);
        Task Increment(string key, CancellationToken cancellationToken = default);
        Task SetOffset(int offset, CancellationToken cancellationToken = default);
    }
    
    
}