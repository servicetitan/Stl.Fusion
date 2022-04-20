using System.Diagnostics;

namespace Stl.CommandR.Diagnostics;

public class TracingCommandHandler : ICommandHandler<ICommand>
{
    private ActivitySource? _activitySource;

    protected ILogger Log { get; init; }
    protected IServiceProvider Services { get; }
    protected ActivitySource ActivitySource {
        get => _activitySource ??= GetType().GetActivitySource();
        init => _activitySource = value;
    }

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
        var activity = ActivitySource.StartActivity(operationName);
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
