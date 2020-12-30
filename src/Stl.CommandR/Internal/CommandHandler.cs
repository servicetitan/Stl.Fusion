using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.CommandR.Internal
{
    public abstract record CommandHandler
    {
        public Type CommandType { get; init; }
        public double Priority { get; init; }

        public abstract Task InvokeAsync(IServiceProvider services, ICommand command, Func<Task> next, CancellationToken cancellationToken);
    }

    public record CommandHandler<TCommand> : CommandHandler
        where TCommand : class, ICommand
    {
        public Type HandlerServiceType { get; init; }

        public CommandHandler() => CommandType = typeof(TCommand);

        public override Task InvokeAsync(IServiceProvider services, ICommand command, Func<Task> next, CancellationToken cancellationToken)
        {
            var handler = (ICommandHandler<TCommand>) services.GetRequiredService(HandlerServiceType);
            return handler.OnCommandAsync((TCommand) command, next, cancellationToken);
        }
    }

    public record DynamicCommandHandler<TCommand> : CommandHandler<TCommand>
        where TCommand : class, ICommand
    {
        public MethodInfo Method { get; init; }

        public override Task InvokeAsync(IServiceProvider services, ICommand command, Func<Task> next, CancellationToken cancellationToken)
        {
            var handler = (ICommandHandler<TCommand>) services.GetRequiredService(HandlerServiceType);
            // ReSharper disable once HeapView.BoxingAllocation
            return (Task) Method.Invoke(handler, new object[] {command, next, cancellationToken})!;
        }
    }
}
