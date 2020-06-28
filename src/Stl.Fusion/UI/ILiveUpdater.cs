using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.UI
{
    public interface ILiveUpdater<T>
    {
        Task<T> UpdateAsync(IComputed<T> prevComputed, CancellationToken cancellationToken);
    }
}
