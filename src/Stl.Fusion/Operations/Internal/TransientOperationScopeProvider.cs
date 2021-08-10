using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.CommandR;
using Stl.CommandR.Commands;
using Stl.CommandR.Configuration;
using Stl.Time;

namespace Stl.Fusion.Operations.Internal
{
    /// <summary>
    /// The outermost, "catch-all" operation provider for commands
    /// that don't use any other operation scopes. Such commands may still
    /// complete successfully & thus require an <see cref="ICompletion"/>-based
    /// notification.
    /// In addition, this scope actually "sends" this notification from
    /// any other (nested) scope.
    /// </summary>
    public class TransientOperationScopeProvider : ICommandHandler<ICommand>
    {
        protected IOperationCompletionNotifier OperationCompletionNotifier { get; }
        protected IServiceProvider Services { get; }
        protected ILogger Log { get; }

        public TransientOperationScopeProvider(
            IServiceProvider services,
            ILogger<TransientOperationScopeProvider>? log = null)
        {
            Log = log ?? NullLogger<TransientOperationScopeProvider>.Instance;
            Services = services;
            OperationCompletionNotifier = services.GetRequiredService<IOperationCompletionNotifier>();
        }

        [CommandHandler(Priority = 10_000, IsFilter = true)]
        public async Task OnCommand(ICommand command, CommandContext context, CancellationToken cancellationToken)
        {
            var operationRequired =
                context.OuterContext == null // Should be top-level command
                && !(command is IMetaCommand) // No operations for "second-order" commands
                && !Computed.IsInvalidating();
            if (!operationRequired) {
                await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
                return;
            }

            await using var scope = Services.GetRequiredService<TransientOperationScope>();
            var operation = scope.Operation;
            operation.Command = command;
            context.Items.Set(scope);
            context.SetOperation(operation);

            try {
                await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
                await scope.Commit(cancellationToken);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                if (scope.IsUsed)
                    Log.LogError(e, "Operation failed: {Command}", command);
                await scope.Rollback();
                throw;
            }

            // Since this is the outermost scope handler, it's reasonable to
            // call OperationCompletionNotifier.NotifyCompleted from it
            var actualOperation = context.Items.GetOrDefault<IOperation>(operation);
            await OperationCompletionNotifier.NotifyCompleted(actualOperation).ConfigureAwait(false);
        }
    }
}
