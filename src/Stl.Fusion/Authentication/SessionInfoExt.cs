namespace Stl.Fusion.Authentication;

public static class SessionInfoExt
{
    public static SessionInfo OrDefault(this SessionInfo? sessionInfo, string sessionId, MomentClockSet? clocks = null)
    {
        if (sessionInfo != null && !sessionInfo.IsSignOutForced)
            return sessionInfo;
        clocks ??= MomentClockSet.Default;
        return new SessionInfo(sessionId, clocks.SystemClock.Now) {
            IsSignOutForced = sessionInfo?.IsSignOutForced ?? false,
        };
    }

    public static SessionInfo OrDefault(this SessionInfo? sessionInfo, string sessionId, Moment now)
    {
        if (sessionInfo != null && !sessionInfo.IsSignOutForced)
            return sessionInfo;
        return new SessionInfo(sessionId, now) {
            IsSignOutForced = sessionInfo?.IsSignOutForced ?? false,
        };
    }
}
