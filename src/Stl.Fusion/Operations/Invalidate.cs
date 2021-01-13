using System;
using System.Reactive;
using Stl.CommandR;
using Stl.Reflection;

namespace Stl.Fusion.Operations
{
    public interface IInvalidate : ICommand
    {
        ICommand UntypedCommand { get; }
    }

    public interface IInvalidate<out TCommand> : ICommand<Unit>, IInvalidate
        where TCommand : ICommand
    {
        TCommand Command { get; }
        ICommand IInvalidate.UntypedCommand => Command;
    }

    public record Invalidate<TCommand>(TCommand Command) : IInvalidate<TCommand>
        where TCommand : ICommand
    {
        public Invalidate(ICommand command) : this((TCommand) command) { }
    }

    public static class Invalidate
    {
        public static IInvalidate New(ICommand command)
        {
            var tInvalidate = typeof(Invalidate<>).MakeGenericType(command.GetType());
            return (IInvalidate) tInvalidate.CreateInstance(command)!;
        }
    }
}
