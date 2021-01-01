using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.CommandR.Configuration
{
    public record ServiceMethodCommandHandler<TCommand> : CommandHandler<TCommand>
        where TCommand : class, ICommand
    {
        public MethodInfo Method { get; }
        public bool HasContextParameter { get; }

        public ServiceMethodCommandHandler(Type handlerServiceType, MethodInfo method)
            : base(handlerServiceType)
        {
            Method = method;
            HasContextParameter = method.GetParameters().Length == 3;
        }

        public override Task InvokeAsync(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            var services = context.ServiceProvider;
            var handler = (ICommandHandler<TCommand>) services.GetRequiredService(HandlerServiceType);
            var parameters = HasContextParameter
                // ReSharper disable once HeapView.BoxingAllocation
                ? new object[] {command, context, cancellationToken}
                // ReSharper disable once HeapView.BoxingAllocation
                : new object[] {command, cancellationToken};
            return (Task) Method.Invoke(handler, parameters)!;
        }
    }
}
