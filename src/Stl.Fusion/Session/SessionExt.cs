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
        => session != null && session != Session.Default;

    public static Session ResolveDefault(this Session? session, ISessionResolver sessionResolver)
        => session.IsDefault() ? sessionResolver.Session : session!;

    public static Session ResolveDefault(this Session? session, IServiceProvider services)
    {
        if (!session.IsDefault())
            return session!;

        var sessionResolver = services.GetRequiredService<ISessionResolver>();
        return sessionResolver.Session;
    }

    public static Session RequireValid(this Session? session)
    {
        if (!session.IsValid())
            throw new ArgumentOutOfRangeException(nameof(session));

        return session;
    }
}
