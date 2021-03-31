using System.Threading;
using System.Threading.Tasks;
using RestEase;

namespace HelloClientServerFx
{
    // ICounterServiceClient tells how ICounterService methods map to HTTP methods.
    // As you'll see further, it's used by Replica Service (ICounterService implementation) on the client.
    [BasePath("counter")]
    public interface ICounterServiceClient
    {
        [Get("get")]
        Task<int> Get(string key, CancellationToken cancellationToken = default);
        [Post("increment")]
        Task Increment(string key, CancellationToken cancellationToken = default);
        [Post("setOffset")]
        Task SetOffset(int offset, CancellationToken cancellationToken = default);
    }
}