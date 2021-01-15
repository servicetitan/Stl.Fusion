using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR.Commands;
using Stl.CommandR.Configuration;

namespace Stl.CommandR.Internal
{
    public class PreprocessedCommandHandler :
        ICommandHandler<IPreprocessedCommand>,
        ICommandHandler<IAsyncPreprocessedCommand>
    {
        [CommandHandler(Priority = 1000_000_001, IsFilter = true)]
        public async Task OnCommandAsync(IPreprocessedCommand command, CommandContext context, CancellationToken cancellationToken)
        {
            command.Preprocess(context);
            await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
        }

        [CommandHandler(Priority = 1000_000_000, IsFilter = true)]
        public async Task OnCommandAsync(IAsyncPreprocessedCommand command, CommandContext context, CancellationToken cancellationToken)
        {
            await command.PreprocessAsync(context, cancellationToken).ConfigureAwait(false);
            await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
