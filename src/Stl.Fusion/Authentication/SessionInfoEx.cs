using System;
using Stl.Time;

namespace Stl.Fusion.Authentication
{
    public static class SessionInfoEx
    {
        public static SessionInfo OrDefault(this SessionInfo? sessionInfo, string sessionId, IMomentClock? clock = null)
        {
            if (sessionInfo != null && !sessionInfo.IsSignOutForced)
                return sessionInfo;
            clock ??= SystemClock.Instance;
            return new SessionInfo(sessionId, clock.Now) {
                IsSignOutForced = sessionInfo?.IsSignOutForced ?? false,
            };
        }
    }
}
