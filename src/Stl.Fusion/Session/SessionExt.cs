using System.Diagnostics.CodeAnalysis;

namespace Stl.Fusion;

public static class SessionExt
{
#if NETSTANDARD2_0
    public static bool IsDefault(this Session? session)
#else
    public static bool IsDefault([NotNullWhen(true)] this Session? session)
#endif
        => session == Session.Default;

#if NETSTANDARD2_0
    public static bool IsValid(this Session? session)
#else
    public static bool IsValid([NotNullWhen(true)] this Session? session)
#endif
        => session != null && Session.Validator.Invoke(session);

    public static Session RequireValid(this Session? session)
        => session.IsValid()
            ? session!
            : throw new ArgumentOutOfRangeException(nameof(session));

    public static Session ResolveDefault(this Session? session, ISessionResolver sessionResolver)
        => session.IsDefault() ? sessionResolver.Session : session.RequireValid();

    public static Session ResolveDefault(this Session? session, IServiceProvider services)
    {
        if (!session.IsDefault())
            return session.RequireValid();

        var sessionResolver = services.GetRequiredService<ISessionResolver>();
        return sessionResolver.Session;
    }
}
