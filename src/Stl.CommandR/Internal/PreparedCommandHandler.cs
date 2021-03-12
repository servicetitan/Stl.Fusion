using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR.Commands;
using Stl.CommandR.Configuration;

namespace Stl.CommandR.Internal
{
    public class PreparedCommandHandler :
        ICommandHandler<IPreparedCommand>,
        ICommandHandler<IAsyncPreparedCommand>
    {
        [CommandHandler(Priority = 1000_000_001, IsFilter = true)]
        public async Task OnCommand(IPreparedCommand command, CommandContext context, CancellationToken cancellationToken)
        {
            command.Prepare(context);
            await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
        }

        [CommandHandler(Priority = 1000_000_000, IsFilter = true)]
        public async Task OnCommand(IAsyncPreparedCommand command, CommandContext context, CancellationToken cancellationToken)
        {
            await command.PrepareAsync(context, cancellationToken).ConfigureAwait(false);
            await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
        }
    }
}
