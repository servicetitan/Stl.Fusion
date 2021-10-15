using Stl.RegisterAttributes;

namespace Stl.Tests.CommandR.Services;

[RegisterService, RegisterCommandHandlers]
public class LogEnterExitService : ServiceBase
{
    public LogEnterExitService(IServiceProvider services) : base(services) { }

    [CommandHandler(1000, IsFilter = true)]
    public async Task OnAnyCommand(
        ICommand command, CommandContext context,
        CancellationToken cancellationToken)
    {
        Log.LogInformation("+ {Command}", command);
        try {
            await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
            await LogResult((dynamic) command);
        }
        catch (Exception e) {
            Log.LogError(e, "- {Command} !-> error", command);
            throw;
        }
    }

    protected async Task LogResult<T>(ICommand<T> command)
    {
        var context = (CommandContext<T>) CommandContext.Current!;
        var resultTask = context.ResultTask;
        if (!resultTask.IsCompleted) {
            Log.LogInformation("- {Command} -> {Result}", command, default(T));
            return;
        }
        var result = await resultTask.ConfigureAwait(false);
        Log.LogInformation("- {Command} -> {Result}", command, result);
    }
}
