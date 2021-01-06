using System.Threading;
using System.Threading.Tasks;

namespace Stl.CommandR
{
    public static class CommanderEx
    {
        // This method doesn't throw exceptions
        public static Task<CommandContext> RunAsync(this ICommander commander,
            ICommand command, CancellationToken cancellationToken = default)
            => commander.RunAsync(command, false, cancellationToken);

        // And this one does
        public static Task<TResult> CallAsync<TResult>(this ICommander commander,
            ICommand<TResult> command,
            CancellationToken cancellationToken = default)
            => commander.CallAsync(command, false, cancellationToken);
    }
}
