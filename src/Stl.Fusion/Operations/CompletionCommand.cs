using System.Reactive;
using Stl.CommandR;
using Stl.CommandR.Commands;
using Stl.Reflection;

namespace Stl.Fusion.Operations
{
    public interface ICompletionCommand : IServerSideCommand<Unit>, IMetaCommand
    {
        IOperation? Operation { get; }
    }

    public interface ICompletionCommand<out TCommand> : IMetaCommand<TCommand>, ICompletionCommand
        where TCommand : class, ICommand
    { }

    public record CompletionCommand<TCommand>(TCommand Command, IOperation? Operation = null)
        : ServerSideCommandBase<Unit>, ICompletionCommand<TCommand>
        where TCommand : class, ICommand
    {
        public CompletionCommand(ICommand command, IOperation? operation)
            : this((TCommand) command, operation) { }
    }

    public static class CompletionCommand
    {
        // This is just to ensure the constructor accepting ICommand is "used",
        // because it is really used inside New, but via reflection.
        private static readonly CompletionCommand<ICommand> DummyCommand = new(null!);

        public static ICompletionCommand New(ICommand command, IOperation? operation = null)
        {
            var tInvalidate = typeof(CompletionCommand<>).MakeGenericType(command.GetType());
            var invalidate = (ICompletionCommand) tInvalidate.CreateInstance(command, operation)!;
            return invalidate.MarkServerSide();
        }
    }
}
