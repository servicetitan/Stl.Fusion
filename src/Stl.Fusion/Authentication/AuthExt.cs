using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication;

public static class AuthExt
{
    public static User MustBeAuthenticated(this User? user)
        => user is { IsAuthenticated: true }
            ? user
            : throw Errors.NotAuthenticated();

    public static TSessionAuthInfo MustBeAuthenticated<TSessionAuthInfo>(this TSessionAuthInfo? authInfo)
        where TSessionAuthInfo : SessionAuthInfo
        => authInfo is { IsAuthenticated: true }
            ? authInfo
            : throw Errors.NotAuthenticated();
}
