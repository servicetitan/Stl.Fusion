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
        protected IMomentClock Clock { get; }
        protected IServiceProvider Services { get; }
        protected ILogger Log { get; }

        public TransientOperationScopeProvider(
            IServiceProvider services,
            ILogger<TransientOperationScopeProvider>? log = null)
        {
            Log = log ?? NullLogger<TransientOperationScopeProvider>.Instance;
            Services = services;
            Clock = services.GetService<IMomentClock>() ?? SystemClock.Instance;
            OperationCompletionNotifier = services.GetRequiredService<IOperationCompletionNotifier>();
        }

        [CommandHandler(Priority = 10_000, IsFilter = true)]
        public async Task OnCommandAsync(ICommand command, CommandContext context, CancellationToken cancellationToken)
        {
            var operationRequired =
                context.OuterContext == null // Should be top-level command
                && !(command is IMetaCommand) // No operations for "second-order" commands
                && !Computed.IsInvalidating();
            if (!operationRequired) {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            await using var scope = Services.GetRequiredService<TransientOperationScope>();
            var operation = scope.Operation;
            operation.Command = command;
            context.Items.Set(scope);
            context.SetOperation(operation);

            try {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                await scope.CommitAsync(cancellationToken);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                if (scope.IsUsed)
                    Log.LogError(e, "Operation failed: {Command}", command);
                await scope.RollbackAsync();
                throw;
            }

            // Since this is the outermost scope handler, it's reasonable to
            // call OperationCompletionNotifier.NotifyCompletedAsync from it
            var actualOperation = context.Items.GetOrDefault<IOperation>(operation);
            await OperationCompletionNotifier.NotifyCompletedAsync(actualOperation).ConfigureAwait(false);
        }
    }
}
