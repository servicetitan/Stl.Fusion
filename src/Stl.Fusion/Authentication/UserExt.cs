using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication;

public static class UserExt
{
    public static User Required(this User? user)
        => user ?? throw new ArgumentNullException(nameof(user));

    public static User AssertAuthenticated(this User? user)
        => user?.IsAuthenticated() ?? false
            ? user
            : throw Errors.NotAuthenticated();

    public static User OrGuest(this User? user, string? name = null)
        => user ?? User.NewGuest(name);
}
