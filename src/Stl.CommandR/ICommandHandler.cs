using System.Threading;
using System.Threading.Tasks;

namespace Stl.CommandR
{
    public interface ICommandHandler
    { }

    public interface ICommandHandler<in TCommand> : ICommandHandler
        where TCommand : class, ICommand
    {
        Task OnCommandAsync(
            TCommand command, CommandContext context,
            CancellationToken cancellationToken);
    }

    public interface ICommandHandler<in TCommand, TResult> : ICommandHandler<TCommand>
        where TCommand : class, ICommand<TResult>
    {
        Task ICommandHandler<TCommand>.OnCommandAsync(
            TCommand command, CommandContext context,
            CancellationToken cancellationToken)
            => OnCommandAsync(command, context, cancellationToken);

        new Task<TResult> OnCommandAsync(
            TCommand command, CommandContext context,
            CancellationToken cancellationToken);
    }
}
