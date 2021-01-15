using System.Reactive;
using Stl.CommandR;
using Stl.CommandR.Commands;
using Stl.Reflection;

namespace Stl.Fusion.Operations
{
    public interface ICompletion : IServerSideCommand<Unit>, IMetaCommand
    {
        IOperation? Operation { get; }
    }

    public interface ICompletion<out TCommand> : IMetaCommand<TCommand>, ICompletion
        where TCommand : class, ICommand
    { }

    public record Completion<TCommand>(TCommand Command, IOperation? Operation = null)
        : ServerSideCommandBase<Unit>, ICompletion<TCommand>
        where TCommand : class, ICommand
    {
        public Completion(ICommand command, IOperation? operation)
            : this((TCommand) command, operation) { }
    }

    public static class Completion
    {
        // This is just to ensure the constructor accepting ICommand is "used",
        // because it is really used inside New, but via reflection.
        private static readonly Completion<ICommand> DummyCompletion = new(null!);

        public static ICompletion New(ICommand command, IOperation? operation = null)
        {
            var tInvalidate = typeof(Completion<>).MakeGenericType(command.GetType());
            var invalidate = (ICompletion) tInvalidate.CreateInstance(command, operation)!;
            return invalidate.MarkServerSide();
        }
    }
}
