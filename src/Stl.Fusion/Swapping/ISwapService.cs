using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Swapping
{
    public interface ISwapService
    {
        ValueTask<IResult?> LoadAsync((InterceptedInput Input, LTag Version) key, CancellationToken cancellationToken = default);
        ValueTask StoreAsync((InterceptedInput Input, LTag Version) key, IResult value, CancellationToken cancellationToken = default);
    }
}
