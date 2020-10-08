using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion
{
    public static class LiveStateEx
    {
        public static void CancelUpdateDelay<TLiveState>(
            this TLiveState liveState, TimeSpan? postCancellationDelay = null)
            where TLiveState : class, ILiveState
            => liveState.UpdateDelayer.CancelDelays(postCancellationDelay);

        public static ValueTask<TLiveState> CancelUpdateDelayAndUpdateAsync<TLiveState>(
            this TLiveState liveState, CancellationToken cancellationToken = default)
            where TLiveState : class, ILiveState
            => liveState.CancelUpdateDelayAndUpdateAsync(null, cancellationToken);

        public static async ValueTask<TLiveState> CancelUpdateDelayAndUpdateAsync<TLiveState>(
            this TLiveState liveState, TimeSpan? postCancellationDelay = null, CancellationToken cancellationToken = default)
            where TLiveState : class, ILiveState
        {
            var delayTask = liveState.CurrentDelayTask;
            if (delayTask != null && !delayTask.IsCompleted) {
                liveState.UpdateDelayer.CancelDelays(postCancellationDelay);
                await delayTask.ConfigureAwait(false);
            }
            return await liveState.UpdateAsync(false, cancellationToken).ConfigureAwait(false);
        }
    }
}
