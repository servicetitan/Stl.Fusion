using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR;
using Stl.CommandR.Configuration;

namespace Stl.Fusion.Operations.Internal
{
    public class CatchAllCompletionHandler : ICommandHandler<ICompletion>
    {
        [CommandHandler(Priority = -1000_000_000, IsFilter = false)]
        public Task OnCommandAsync(ICompletion command, CommandContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
