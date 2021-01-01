using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.CommandR.Configuration
{
    public record ServiceMethodCommandHandler<TCommand> : CommandHandler<TCommand>
        where TCommand : class, ICommand
    {
        public MethodInfo Method { get; init; } = null!;

        public override Task InvokeAsync(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            var services = context.ServiceProvider;
            var handler = (ICommandHandler<TCommand>) services.GetRequiredService(HandlerServiceType);
            // ReSharper disable once HeapView.BoxingAllocation
            return (Task) Method.Invoke(handler, new object[] {command, context, cancellationToken})!;
        }
    }
}
