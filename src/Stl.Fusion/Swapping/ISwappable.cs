using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Swapping
{
    public interface ISwappable
    {
        ValueTask SwapAsync(CancellationToken cancellationToken = default);
    }
}
