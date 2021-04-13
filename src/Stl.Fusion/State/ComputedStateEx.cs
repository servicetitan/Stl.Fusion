using System.Threading.Tasks;

namespace Stl.Fusion
{
    public static class ComputedStateEx
    {
        public static async Task ApplyUserCausedUpdate(this IComputedState computedState)
        {
            var snapshot = computedState.Snapshot;
            var computed = snapshot.Computed;
            await computedState.UpdateDelayer.UserCausedUpdateDelay(snapshot).ConfigureAwait(false);
            if (!computed.IsInvalidated())
                return; // Nothing came through yet, so no reason to update
            if (!snapshot.WhenUpdated().IsCompleted)
                await computed.Update().ConfigureAwait(false);
        }
    }
}
