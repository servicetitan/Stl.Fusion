using System.Threading.Tasks;

namespace Stl.Fusion
{
    public static class LiveStateEx
    {
        public static async Task ApplyUserCausedUpdate(this ILiveState liveState)
        {
            var snapshot = liveState.Snapshot;
            await liveState.LiveStateTimer.UserCausedUpdateDelay(snapshot.Computed).ConfigureAwait(false);
            if (!snapshot.WhenUpdated().IsCompleted)
                await snapshot.Computed.Update().ConfigureAwait(false);
        }
    }
}
