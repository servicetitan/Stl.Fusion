using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Collections;
using Stl.CommandR;
using Stl.CommandR.Commands;
using Stl.CommandR.Configuration;
using Stl.Time;

namespace Stl.Fusion.Operations.Internal
{
    /// <summary>
    /// This handler captures invocations of nested commands inside
    /// operations and logs them into context.Operation().Items
    /// so that invalidation for them could be auto-replayed too.
    /// </summary>
    public class NestedCommandLogger : ICommandHandler<ICommand>
    {
        protected CommandContext? CommandContext { get; }
        protected IMomentClock Clock { get; }
        protected IServiceProvider Services { get; }
        protected ILogger Log { get; }

        public NestedCommandLogger(
            IServiceProvider services,
            ILogger<InvalidateOnCompletionCommandHandler>? log = null)
        {
            Log = log ?? NullLogger<InvalidateOnCompletionCommandHandler>.Instance;
            Services = services;
            CommandContext = services.GetService<CommandContext>();
        }

        [CommandHandler(Priority = 11_000, IsFilter = true)]
        public async Task OnCommandAsync(ICommand command, CommandContext context, CancellationToken cancellationToken)
        {
            var operation = context.OuterContext != null ? context.Items.TryGet<IOperation>() : null;
            var isNestedCommand =
                operation != null // Should be a nested context inside a context w/ operation
                && !(command is IMetaCommand) // No operations for "second-order" commands
                && !Computed.IsInvalidating();
            if (!isNestedCommand) {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            var operationItems = operation!.Items;
            var commandItems = new OptionSet();
            operation.Items = commandItems;
            Exception? error = null;
            try {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception? e) {
                error = e;
                throw;
            }
            finally {
                operation.Items = operationItems;
                if (error == null) {
                    var nestedCommands = operationItems.GetOrDefault(ImmutableList<NestedCommand>.Empty);
                    nestedCommands = nestedCommands.Add(new NestedCommand(command, commandItems));
                    operationItems.Set(nestedCommands);
                }
            }
        }
    }
}
