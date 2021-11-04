namespace Stl.Fusion.Authentication;

public static class SessionInfoExt
{
    public static SessionInfo OrDefault(this SessionInfo? sessionInfo, Symbol sessionId, MomentClockSet? clocks = null)
    {
        if (sessionInfo is { IsSignOutForced: false })
            return sessionInfo;
        clocks ??= MomentClockSet.Default;
        return new SessionInfo(sessionId, clocks.SystemClock.Now) {
            IsSignOutForced = sessionInfo?.IsSignOutForced ?? false,
        };
    }

    public static SessionInfo OrDefault(this SessionInfo? sessionInfo, Symbol sessionId, Moment now)
    {
        if (sessionInfo is { IsSignOutForced: false })
            return sessionInfo;
        return new SessionInfo(sessionId, now) {
            IsSignOutForced = sessionInfo?.IsSignOutForced ?? false,
        };
    }
}
