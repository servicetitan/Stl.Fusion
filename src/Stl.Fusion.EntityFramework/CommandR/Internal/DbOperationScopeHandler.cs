using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.CommandR;
using Stl.CommandR.Configuration;
using Stl.Fusion.CommandR;

namespace Stl.Fusion.EntityFramework.CommandR.Internal
{
    public class DbOperationScopeHandler<TDbContext> : DbServiceBase<TDbContext>, ICommandHandler<ICommand>
        where TDbContext : DbContext
    {
        protected IDbOperationNotifier<TDbContext>? DbOperationNotifier { get; }

        public DbOperationScopeHandler(
            IDbOperationNotifier<TDbContext>? dbOperationNotifier,
            IServiceProvider services)
            : base(services)
            => DbOperationNotifier = dbOperationNotifier;

        [CommandHandler(Order = -1000, IsFilter = true)]
        public async Task OnCommandAsync(ICommand command, CommandContext context, CancellationToken cancellationToken)
        {
            var existingScope = context.Items.TryGet<IDbOperationScope<TDbContext>>();
            if (existingScope != null) {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            var logEnabled = Log.IsEnabled(LogLevel.Debug);
            await using var scope = Services.GetRequiredService<IDbOperationScope<TDbContext>>();
            context.Items.Set(scope);
            if (logEnabled)
                Log.LogDebug("+ Operation started: {0}", command);

            IDbOperation? dbOperation = null;
            try {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                dbOperation = await scope.CommitAsync(command, cancellationToken);
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

            if (dbOperation != null && DbOperationNotifier != null) {
                // Instruct InvalidationHandler to skip the invalidation
                context.Items.Remove<IInvalidate>();
                // Send the notification
                DbOperationNotifier.NotifyConfirmedOperation(dbOperation);
            }
        }
    }
}
