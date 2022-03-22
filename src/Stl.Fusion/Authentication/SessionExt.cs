using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication;

public static class SessionExt
{
    public static Session AssertNotNull(this Session? session)
        => session ?? throw Errors.NoSessionProvided();

    public static bool IsDefault(this Session? session)
        => session == null || session == Session.Default;

    public static Session ResolveDefault(this Session? session, ISessionResolver sessionResolver) 
        => session.IsDefault() ? sessionResolver.Session : session!;

    public static Session ResolveDefault(this Session? session, IServiceProvider services)
    {
        if (!session.IsDefault())
            return session!;
        var sessionResolver = services.GetRequiredService<ISessionResolver>();
        return sessionResolver.Session;
    }
}
