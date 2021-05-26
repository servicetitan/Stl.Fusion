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
        #if !NETSTANDARD2_0
        // TODO: Can this skip affect command handlers search?
        Task ICommandHandler<TCommand>.OnCommand(
            TCommand command, CommandContext context,
            CancellationToken cancellationToken)
            => OnCommand(command, (CommandContext<TResult>) context, cancellationToken);
        #endif

        Task<TResult> OnCommand(
            TCommand command, CommandContext<TResult> context,
            CancellationToken cancellationToken);
    }
}
