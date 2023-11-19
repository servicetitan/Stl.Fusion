using System.Diagnostics.CodeAnalysis;
using Stl.CommandR.Internal;

namespace Stl.CommandR;

public static class CommanderExt
{
    private static readonly ConcurrentDictionary<Type, Func<ICommander, ICommand, bool, CancellationToken, Task>> TypedCallCache = new();
    private static readonly MethodInfo TypedCallMethod = typeof(CommanderExt)
        .GetMethod(nameof(TypedCall), BindingFlags.Static | BindingFlags.NonPublic)!;

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

    public static Task<TResult> Call<TResult>(this ICommander commander,
        ICommand<TResult> command, bool isOutermost, CancellationToken cancellationToken = default)
        => TypedCall<TResult>(commander, command, isOutermost, cancellationToken);

    public static Task Call(this ICommander commander,
        ICommand command, bool isOutermost, CancellationToken cancellationToken = default)
        => GetTypedCallInvoker(command.GetResultType())
            .Invoke(commander, command, isOutermost, cancellationToken);

    public static Task<TResult> Call<TResult>(this ICommander commander,
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
        => TypedCall<TResult>(commander, command, false, cancellationToken);

    public static Task Call(this ICommander commander,
        ICommand command,
        CancellationToken cancellationToken = default)
        => GetTypedCallInvoker(command.GetResultType())
            .Invoke(commander, command, false, cancellationToken);

    // Private methods

    private static Func<ICommander, ICommand, bool, CancellationToken, Task> GetTypedCallInvoker(Type commandResultType)
        => TypedCallCache.GetOrAdd(
            commandResultType,
            tResult => {
                var delegateType = typeof(Func<,,,,>)
                    .MakeGenericType(
                        typeof(ICommander),
                        typeof(ICommand),
                        typeof(bool),
                        typeof(CancellationToken),
                        typeof(Task<>).MakeGenericType(tResult));
                return (Func<ICommander, ICommand, bool, CancellationToken, Task>)TypedCallMethod
                    .MakeGenericMethod(tResult)
                    .CreateDelegate(delegateType);
            });

    private static async Task<TResult> TypedCall<TResult>(
        ICommander commander,
        ICommand command,
        bool isOutermost,
        CancellationToken cancellationToken = default)
    {
        var context = await commander.Run(command, isOutermost, cancellationToken).ConfigureAwait(false);
        var typedContext = (CommandContext<TResult>)context;
        return await typedContext.ResultSource.Task.ConfigureAwait(false);
    }
}
