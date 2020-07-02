using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.UI
{
    public interface ILiveUpdater // Just a tagging interface to simplify assembly scanning
    { }

    public interface ILiveUpdater<T> : ILiveUpdater
    {
        Task<T> UpdateAsync(ILive<T> live, CancellationToken cancellationToken);
    }
}
