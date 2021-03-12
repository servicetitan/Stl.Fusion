using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.CommandR;
using Stl.CommandR.Commands;
using Stl.CommandR.Configuration;
using Stl.Fusion.Operations;

namespace Stl.Fusion.EntityFramework.Operations
{
    public class DbOperationScopeProvider<TDbContext> : DbServiceBase<TDbContext>, ICommandHandler<ICommand>
        where TDbContext : DbContext
    {
        protected IOperationCompletionNotifier OperationCompletionNotifier { get; }

        public DbOperationScopeProvider(IServiceProvider services) : base(services)
            => OperationCompletionNotifier = services.GetRequiredService<IOperationCompletionNotifier>();

        [CommandHandler(Priority = 1000, IsFilter = true)]
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

            var tScope = typeof(DbOperationScope<TDbContext>);
            if (context.Items[tScope] != null) // Safety check
                throw Stl.Internal.Errors.InternalError($"'{tScope}' scope is already provided. Duplicate handler?");

            await using var scope = Services.GetRequiredService<DbOperationScope<TDbContext>>();
            var operation = scope.Operation;
            operation.Command = command;
            context.Items.Set(scope);

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
                try {
                    await scope.Rollback();
                }
                catch {
                    // Intended
                }
                throw;
            }
        }
    }
}
