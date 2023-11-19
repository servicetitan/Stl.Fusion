using Stl.Fusion.Operations.Internal;

namespace Stl.Fusion.Operations;

public interface ICompletion : IMetaCommand, IBackendCommand
{
    IOperation Operation { get; }
}

public interface ICompletion<out TCommand> : IMetaCommand<TCommand>, ICompletion
    where TCommand : class, ICommand;

public record Completion<TCommand>(TCommand Command, IOperation Operation)
    : ICompletion<TCommand>
    where TCommand : class, ICommand
{
#if NETSTANDARD2_0
    ICommand IMetaCommand.UntypedCommand => Command;
#endif

    public Completion(IOperation operation)
        : this((TCommand)(operation.Command ?? throw Errors.OperationHasNoCommand(nameof(operation))), operation)
    { }
}

public static class Completion
{
    // This is just to ensure the constructor accepting ICommand is "used",
    // because it is really used inside New, but via reflection.
#pragma warning disable CA1823
    private static readonly Completion<ICommand> DummyCompletion =
        new(new TransientOperation() { Command = new DummyCommand() });
#pragma warning restore CA1823

    public static ICompletion New(IOperation operation)
    {
        var command = (ICommand?)operation.Command
            ?? throw Errors.OperationHasNoCommand(nameof(operation));
        var tCompletion = typeof(Completion<>).MakeGenericType(command.GetType());
        var completion = (ICompletion)tCompletion.CreateInstance(operation)!;
        return completion;
    }

    // Nested types

    private sealed record DummyCommand : ICommand;
}
