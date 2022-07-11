namespace Stl.Fusion.Authentication;

public static class AuthExt
{
    public static async ValueTask<User> RequireUser(this IAuth auth, Session session, CancellationToken cancellationToken)
    {
        var user = await auth.GetUser(session, cancellationToken).ConfigureAwait(false);
        return user.Required();
    }

    public static async ValueTask<User> RequireUser(this IAuth auth, Session session, bool throwResultException, CancellationToken cancellationToken)
    {
        var user = await auth.GetUser(session, cancellationToken).ConfigureAwait(false);
        return user.Required(throwResultException);
    }
}
