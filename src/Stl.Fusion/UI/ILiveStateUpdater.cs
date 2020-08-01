using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.UI
{
    public interface ILiveStateUpdater // Just a tagging interface to simplify assembly scanning
    { }

    public interface ILiveStateUpdater<TState> : ILiveStateUpdater
    {
        Task<TState> UpdateAsync(ILiveState<TState> liveState, CancellationToken cancellationToken);
    }

    public interface ILiveStateUpdater<TLocal, TState> : ILiveStateUpdater
    {
        Task<TState> UpdateAsync(ILiveState<TLocal, TState> liveState, CancellationToken cancellationToken);
    }
}
