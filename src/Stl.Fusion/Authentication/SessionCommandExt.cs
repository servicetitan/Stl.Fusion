using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion.Authentication;

public static class SessionCommandExt
{
    private static readonly PropertyInfo SessionProperty = typeof(ISessionCommand).GetProperty(nameof(Session))!;

    public static TCommand UseDefaultSession<TCommand>(this TCommand command, ISessionResolver sessionResolver)
        where TCommand : class, ISessionCommand
    {
        if (command.Session != null!)
            return command;
        // The property has init accessor, so we have to workaround this
        SessionProperty.SetValue(command, sessionResolver.Session);
        return command;
    }

    public static TCommand UseDefaultSession<TCommand>(this TCommand command, IServiceProvider services)
        where TCommand : class, ISessionCommand
    {
        if (command.Session != null!)
            return command;
        var sessionResolver = services.GetRequiredService<ISessionResolver>();
        // The property has init accessor, so we have to workaround this
        SessionProperty.SetValue(command, sessionResolver.Session);
        return command;
    }
}
