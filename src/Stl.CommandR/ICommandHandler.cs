using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.CommandR
{
    public interface ICommandHandler<in TCommand>
        where TCommand : class, ICommand
    {
        Task OnCommandAsync(
            TCommand command, CommandContext context,
            CancellationToken cancellationToken);
    }
}
