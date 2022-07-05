using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication;

public static class SessionInfoExt
{
    public static SessionInfo AssertAuthenticated(this SessionInfo? sessionInfo)
        => sessionInfo is { IsAuthenticated: true }
            ? sessionInfo
            : throw Errors.NotAuthenticated();
}
