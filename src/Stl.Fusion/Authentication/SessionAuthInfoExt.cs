using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication;

public static class SessionAuthInfoExt
{
    public static SessionAuthInfo AssertAuthenticated(this SessionAuthInfo? sessionAuthInfo)
        => sessionAuthInfo?.IsAuthenticated() ?? false
            ? sessionAuthInfo
            : throw Errors.NotAuthenticated();
}
