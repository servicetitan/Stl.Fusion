using System.Diagnostics;
using Stl.CommandR.Internal;

namespace Stl.CommandR.Diagnostics;

public class CommandTracer(IServiceProvider services) : ICommandHandler<ICommand>
{
    private ActivitySource? _activitySource;
    private ILogger? _log;

    protected IServiceProvider Services { get; } = services;
    protected ActivitySource ActivitySource {
        get => _activitySource ??= GetType().GetActivitySource();
        init => _activitySource = value;
    }
    protected ILogger Log {
        get => _log ??= Services.LogFor(GetType());
        init => _log = value;
    }

    public LogLevel ErrorLogLevel { get; init; } = LogLevel.Error;

    [CommandFilter(Priority = CommanderCommandHandlerPriority.CommandTracer)]
    public async Task OnCommand(ICommand command, CommandContext context, CancellationToken cancellationToken)
    {
        using var activity = StartActivity(command, context);
        try {
            await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) {
            if (activity != null && Log.IsEnabled(ErrorLogLevel)) {
                var message = context.IsOutermost ?
                    "Outermost command failed: {Command}" :
                    "Nested command failed: {Command}";
                // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                Log.Log(ErrorLogLevel, e, message, command);
            }
        }
    }

    protected virtual Activity? StartActivity(ICommand command, CommandContext context)
    {
        if (!ShouldTrace(command, context))
            return null;

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
        if (context.IsOutermost)
            return true;

        // Do not trace meta commands & any nested command they run
        for (var c = context; c != null; c = c.OuterContext)
            if (c.UntypedCommand is IMetaCommand)
                return false;

        // Trace the rest
        return true;
    }
}
