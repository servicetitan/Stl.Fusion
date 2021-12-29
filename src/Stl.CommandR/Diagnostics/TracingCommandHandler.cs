using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Stl.CommandR.Diagnostics;

public class TracingCommandHandler : ICommandHandler<ICommand>
{
    protected IServiceProvider Services { get; }
    protected ILogger Log { get; }

    public TracingCommandHandler(
        IServiceProvider services,
        ILogger<TracingCommandHandler>? log = null)
    {
        Log = log ?? NullLogger<TracingCommandHandler>.Instance;
        Services = services;
    }

    [CommandHandler(Priority = 998_000_000, IsFilter = true)]
    public async Task OnCommand(ICommand command, CommandContext context, CancellationToken cancellationToken)
    {
        using var activity = StartActivity(command, context);
        await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
    }

    protected virtual Activity? StartActivity(ICommand command, CommandContext context)
    {
        if (!ShouldTrace(command, context)) return null;

        var operationName = command.GetType().GetOperationName("Run");
        var activity = CommanderTrace.StartActivity(operationName);
        if (activity != null) {
            var tags = new ActivityTagsCollection { { "command", command.ToString() } };
            var activityEvent = new ActivityEvent(operationName, tags: tags);
            activity.AddEvent(activityEvent);
        }
        return activity;
    }

    protected virtual bool ShouldTrace(ICommand command, CommandContext context)
    {
        // Always trace top-level commands
        if (context.OuterContext == null)
            return true;

        // Do not trace meta commands & any nested command they run
        for (var c = context; c != null; c = c.OuterContext)
            if (c.UntypedCommand is IMetaCommand)
                return false;

        // Trace the rest
        return true;
    }
}
