using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication;

public static class UserExt
{
    public static User Required(this User? user, bool throwResultException = false)
        => user ?? throw new ArgumentNullException(nameof(user)).MaybeToResult(throwResultException);

    public static User AssertAuthenticated(this User? user, bool throwResultException = false)
        => user?.IsAuthenticated() ?? false
            ? user
            : throw Errors.NotAuthenticated().MaybeToResult(throwResultException);

    public static User OrGuest(this User? user, string? name = null)
        => user ?? User.NewGuest(name);
}
