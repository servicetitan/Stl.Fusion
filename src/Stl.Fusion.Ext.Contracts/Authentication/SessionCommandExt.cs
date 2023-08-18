namespace Stl.Fusion.Authentication;

public static class SessionCommandExt
{
    private static readonly PropertyInfo SessionProperty = typeof(ISessionCommand).GetProperty(nameof(Session))!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TCommand SetSession<TCommand>(this TCommand command, Session session)
        where TCommand : class, ISessionCommand
    {
        // The property has init accessor, so we have to workaround this
        SessionProperty.SetValue(command, session);
        return command;
    }

    public static TCommand UseDefaultSession<TCommand>(this TCommand command, ISessionResolver sessionResolver)
        where TCommand : class, ISessionCommand
        => command.Session.IsDefault() ? command.SetSession(sessionResolver.Session) : command;

    public static TCommand UseDefaultSession<TCommand>(this TCommand command, IServiceProvider services)
        where TCommand : class, ISessionCommand
    {
        if (!command.Session.IsDefault())
            return command;

        var sessionResolver = services.GetRequiredService<ISessionResolver>();
        return command.SetSession(sessionResolver.Session);
    }
}
