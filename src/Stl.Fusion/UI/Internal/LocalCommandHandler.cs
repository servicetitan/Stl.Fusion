using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR;
using Stl.CommandR.Configuration;

namespace Stl.Fusion.UI.Internal
{
    public class LocalCommandHandler : ICommandHandler<LocalCommand>
    {
        [CommandHandler(Priority = 900_000_000, IsFilter = false)]
        public Task OnCommand(
            LocalCommand command, CommandContext context,
            CancellationToken cancellationToken)
            => command.Handler!.Invoke();
    }
}
