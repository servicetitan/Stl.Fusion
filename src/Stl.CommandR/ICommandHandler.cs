using System.Threading;
using System.Threading.Tasks;

namespace Stl.CommandR
{
    public interface ICommandHandler
    { }

    public interface ICommandHandler<in TCommand> : ICommandHandler
        where TCommand : class, ICommand
    {
        Task OnCommand(
            TCommand command, CommandContext context,
            CancellationToken cancellationToken);
    }

    public interface ICommandHandler<in TCommand, TResult> : ICommandHandler<TCommand>
        where TCommand : class, ICommand<TResult>
    {
        Task ICommandHandler<TCommand>.OnCommand(
            TCommand command, CommandContext context,
            CancellationToken cancellationToken)
            => OnCommand(command, (CommandContext<TResult>) context, cancellationToken);

        Task<TResult> OnCommand(
            TCommand command, CommandContext<TResult> context,
            CancellationToken cancellationToken);
    }
}
