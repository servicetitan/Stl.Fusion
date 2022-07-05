using System.Diagnostics.CodeAnalysis;
using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication;

public static class SessionInfoExt
{
    public static SessionInfo AssertAuthenticated(this SessionInfo? sessionInfo)
        => sessionInfo.IsAuthenticated()
            ? sessionInfo!
            : throw Errors.NotAuthenticated();

#if !NETSTANDARD2_0
    [return: NotNullIfNotNull("sessionInfo")]
#endif
    public static SessionAuthInfo? ToAuthInfo(this SessionInfo? sessionInfo)
    {
        if (sessionInfo == null)
            return null;
        if (sessionInfo.IsSignOutForced)
            return new SessionAuthInfo() {
                SessionHash = sessionInfo.SessionHash,
                IsSignOutForced = true,
            };
        return new() {
            SessionHash = sessionInfo.SessionHash,
            AuthenticatedIdentity = sessionInfo.AuthenticatedIdentity,
            UserId = sessionInfo.UserId,
            IsSignOutForced = sessionInfo.IsSignOutForced,
        };
    }
}
