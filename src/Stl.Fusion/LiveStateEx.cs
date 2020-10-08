using System;

namespace Stl.Fusion
{
    public static class LiveStateEx
    {
        public static void CancelUpdateDelay<TLiveState>(
            this TLiveState liveState, TimeSpan? postCancellationDelay = null)
            where TLiveState : class, ILiveState
            => liveState.UpdateDelayer.CancelDelays(postCancellationDelay);
    }
}
