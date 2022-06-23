namespace Stl.CommandR;

public static class CommanderExt
{
    // Start overloads

    public static CommandContext Start(this ICommander commander,
        ICommand command, CancellationToken cancellationToken = default)
        => commander.Start(command, false, cancellationToken);

    public static CommandContext Start(this ICommander commander,
        ICommand command, bool isOutermost, CancellationToken cancellationToken = default)
    {
        var context = CommandContext.New(commander, command, isOutermost);
        _ = commander.Run(context, cancellationToken);
        return context;
    }

    // Run overloads

    public static Task<CommandContext> Run(this ICommander commander,
        ICommand command, CancellationToken cancellationToken = default)
        => commander.Run(command, false, cancellationToken);

    public static async Task<CommandContext> Run(this ICommander commander,
        ICommand command, bool isOutermost, CancellationToken cancellationToken = default)
    {
        var context = CommandContext.New(commander, command, isOutermost);
        await commander.Run(context, cancellationToken).ConfigureAwait(false);
        return context;
    }

    // Call overloads

    public static async Task<TResult> Call<TResult>(this ICommander commander,
        ICommand<TResult> command, bool isOutermost, CancellationToken cancellationToken = default)
    {
        var context = await commander.Run(command, isOutermost, cancellationToken).ConfigureAwait(false);
        var typedContext = (CommandContext<TResult>) context;
        return await typedContext.ResultTask.ConfigureAwait(false);
    }

    public static async Task Call(this ICommander commander,
        ICommand command, bool isOutermost, CancellationToken cancellationToken = default)
    {
        var context = await commander.Run(command, isOutermost, cancellationToken).ConfigureAwait(false);
        await context.UntypedResultTask.ConfigureAwait(false);
    }

    public static Task<TResult> Call<TResult>(this ICommander commander,
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
        => commander.Call(command, isOutermost: false, cancellationToken);

    public static Task Call(this ICommander commander,
        ICommand command,
        CancellationToken cancellationToken = default)
        => commander.Call(command, isOutermost: false, cancellationToken);
}
