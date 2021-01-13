using System;
using System.Reactive;
using System.Text.Json.Serialization;
using Stl.CommandR;
using Stl.Reflection;

namespace Stl.Fusion.CommandR
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

    [Serializable]
    public class Invalidate<TCommand> : IInvalidate<TCommand>
        where TCommand : ICommand
    {
        public TCommand Command { get; init; }

        [JsonConstructor]
        public Invalidate(TCommand command) => Command = command;
    }

    public static class Invalidate
    {
        public static IInvalidate New(ICommand command)
        {
            var tInvalidate = typeof(Invalidate<>).MakeGenericType(command.GetType());
            return (IInvalidate) tInvalidate.CreateInstance(command);
        }
    }
}
