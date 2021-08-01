using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR;

namespace Stl.Fusion.UI.Internal
{
    public class LocalCommandHandler
    {
        public Task OnCommand(
            LocalCommand command, CommandContext context,
            CancellationToken cancellationToken)
            => command.Handler?.Invoke() ?? Task.CompletedTask;
    }
}
