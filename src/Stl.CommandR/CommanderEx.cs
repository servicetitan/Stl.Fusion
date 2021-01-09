using System.Threading;
using System.Threading.Tasks;

namespace Stl.CommandR
{
    public static class CommanderEx
    {
        // Start & RunAsync overloads w/o "isolate" parameter

        public static CommandContext Start(this ICommander commander,
            ICommand command, CancellationToken cancellationToken = default)
            => commander.Start(command, false, cancellationToken);

        public static Task<CommandContext> RunAsync(this ICommander commander,
            ICommand command, CancellationToken cancellationToken = default)
            => commander.RunAsync(command, false, cancellationToken);

        // CallAsync overloads

        public static async Task<TResult> CallAsync<TResult>(this ICommander commander,
            ICommand<TResult> command, bool isolate, CancellationToken cancellationToken = default)
        {
            var context = commander.Start(command, isolate, cancellationToken);
            var typedContext = (CommandContext<TResult>) context;
            return await typedContext.ResultTask.ConfigureAwait(false);
        }

        public static async Task CallAsync(this ICommander commander,
            ICommand command, bool isolate, CancellationToken cancellationToken = default)
        {
            var context = commander.Start(command, isolate, cancellationToken);
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
