using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.CommandR;
using Stl.CommandR.Configuration;
using Stl.Fusion.Operations;

namespace Stl.Fusion.EntityFramework.Internal
{
    public class DbOperationScopeHandler<TDbContext> : DbServiceBase<TDbContext>, ICommandHandler<ICommand>
        where TDbContext : DbContext
    {
        protected IOperationCompletionNotifier? OperationCompletionNotifier { get; }

        public DbOperationScopeHandler(
            IOperationCompletionNotifier? operationCompletionNotifier,
            IServiceProvider services)
            : base(services)
            => OperationCompletionNotifier = operationCompletionNotifier;

        [CommandHandler(Order = -1000, IsFilter = true)]
        public async Task OnCommandAsync(ICommand command, CommandContext context, CancellationToken cancellationToken)
        {
            var skip = context.OuterContext != null // Should be top-level command
                || command is IInvalidate // Second handler here will take care of it
                || Computed.IsInvalidating();
            if (skip) {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            var tScope = typeof(IDbOperationScope<TDbContext>);
            if (context.Items[tScope] != null) // Safety check
                throw Stl.Internal.Errors.InternalError($"'{tScope}' scope is already provided. Duplicate handler?");

            var logEnabled = Log.IsEnabled(LogLevel.Debug);
            await using var scope = Services.GetRequiredService<IDbOperationScope<TDbContext>>();
            context.Items.Set(scope);
            if (logEnabled)
                Log.LogDebug("+ Operation started: {0}", command);

            IOperation? operation = null;
            try {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                operation = await scope.CommitAsync(command, cancellationToken);
                if (logEnabled)
                    Log.LogDebug("- Operation succeeded: {0}", command);
            }
            catch (Exception e) {
                Log.LogError(e, "! Operation failed: {0}", command);
                try {
                    await scope.RollbackAsync();
                }
                catch {
                    // Intended
                }
                throw;
            }
            if (operation != null)
                OperationCompletionNotifier?.NotifyCompleted(operation);
        }
    }
}
