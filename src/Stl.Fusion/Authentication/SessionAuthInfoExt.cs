using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication;

public static class SessionAuthInfoExt
{
    public static bool IsAuthenticated(this SessionAuthInfo? sessionAuthInfo)
    {
        if (sessionAuthInfo is null)
            return false;
        return !(sessionAuthInfo.IsSignOutForced || sessionAuthInfo.UserId.IsNullOrEmpty());
    }

    public static SessionAuthInfo AssertAuthenticated(this SessionAuthInfo? sessionAuthInfo)
        => sessionAuthInfo.IsAuthenticated()
            ? sessionAuthInfo!
            : throw Errors.NotAuthenticated();
}
