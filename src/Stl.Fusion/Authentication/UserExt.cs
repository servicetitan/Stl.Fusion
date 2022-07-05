using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication;

public static class UserExt
{
    public static User AssertNotNull(this User? user)
        => user ?? throw Errors.NotAuthenticated();

    public static User OrGuest(this User? user, string? name = null)
        => user ?? User.NewGuest(name);
    public static User OrGuest(this User? user, Session session, string? name = null)
        => user ?? User.NewGuest(session, name);
}
