using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication;

public static class SessionAuthInfoExt
{
    public static SessionAuthInfo AssertAuthenticated(this SessionAuthInfo? sessionAuthInfo)
        => sessionAuthInfo is { IsAuthenticated: true }
            ? sessionAuthInfo
            : throw Errors.NotAuthenticated();
}
