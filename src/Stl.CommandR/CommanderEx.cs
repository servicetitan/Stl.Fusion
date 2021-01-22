using System.Threading;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.CommandR
{
    public static class CommanderEx
    {
        // Start overloads

        public static CommandContext Start(this ICommander commander,
            ICommand command, CancellationToken cancellationToken = default)
            => commander.Start(command, false, cancellationToken);

        public static CommandContext Start(this ICommander commander,
            ICommand command, bool isolate, CancellationToken cancellationToken = default)
        {
            var context = CommandContext.New(commander, command, isolate);
            commander.RunAsync(context, isolate, cancellationToken).Ignore();
            return context;
        }

        // RunAsync overloads

        public static Task<CommandContext> RunAsync(this ICommander commander,
            ICommand command, CancellationToken cancellationToken = default)
            => commander.RunAsync(command, false, cancellationToken);

        public static async Task<CommandContext> RunAsync(this ICommander commander,
            ICommand command, bool isolate, CancellationToken cancellationToken = default)
        {
            var context = CommandContext.New(commander, command, isolate);
            await commander.RunAsync(context, isolate, cancellationToken).ConfigureAwait(false);
            return context;
        }

        // CallAsync overloads

        public static async Task<TResult> CallAsync<TResult>(this ICommander commander,
            ICommand<TResult> command, bool isolate, CancellationToken cancellationToken = default)
        {
            var context = await commander.RunAsync(command, isolate, cancellationToken).ConfigureAwait(false);
            var typedContext = (CommandContext<TResult>) context;
            return await typedContext.ResultTask.ConfigureAwait(false);
        }

        public static async Task CallAsync(this ICommander commander,
            ICommand command, bool isolate, CancellationToken cancellationToken = default)
        {
            var context = await commander.RunAsync(command, isolate, cancellationToken).ConfigureAwait(false);
            await context.UntypedResultTask.ConfigureAwait(false);
        }

        public static Task<TResult> CallAsync<TResult>(this ICommander commander,
            ICommand<TResult> command,
            CancellationToken cancellationToken = default)
            => commander.CallAsync(command, false, cancellationToken);

        public static Task CallAsync(this ICommander commander,
            ICommand command,
            CancellationToken cancellationToken = default)
            => commander.CallAsync(command, false, cancellationToken);
    }
}
