using System.Reactive;
using Stl.CommandR;
using Stl.CommandR.Commands;
using Stl.Reflection;

namespace Stl.Fusion.Operations
{
    public interface IInvalidateCommand : IServerSideCommand<Unit>
    {
        ICommand UntypedCommand { get; }
        IOperation? Operation { get; }
    }

    public interface IInvalidateCommand<out TCommand> : IInvalidateCommand
        where TCommand : class, ICommand
    {
        TCommand Command { get; }
        ICommand IInvalidateCommand.UntypedCommand => Command;
    }

    public record InvalidateCommand<TCommand>(TCommand Command, IOperation? Operation = null)
        : ServerSideCommandBase<Unit>, IInvalidateCommand<TCommand>
        where TCommand : class, ICommand
    {
        public InvalidateCommand(ICommand command, IOperation? operation)
            : this((TCommand) command, operation) { }
    }

    public static class InvalidateCommand
    {
        // This is just to ensure the constructor accepting ICommand is "used",
        // because it is really used inside New, but via reflection.
        private static readonly InvalidateCommand<ICommand> DummyCommand = new(null!);

        public static IInvalidateCommand New(ICommand command, IOperation? operation = null)
        {
            var tInvalidate = typeof(InvalidateCommand<>).MakeGenericType(command.GetType());
            var invalidate = (IInvalidateCommand) tInvalidate.CreateInstance(command, operation)!;
            return invalidate.MarkServerSide();
        }
    }
}
