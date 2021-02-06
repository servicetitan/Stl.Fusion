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
        public class Options
        {
            public LogLevel LogLevel { get; set; } = LogLevel.None;
        }

        protected IOperationCompletionNotifier OperationCompletionNotifier { get; }
        protected LogLevel LogLevel { get; }

        public DbOperationScopeProvider(
            Options? options,
            IServiceProvider services)
            : base(services)
        {
            options ??= new();
            LogLevel = options.LogLevel;
            OperationCompletionNotifier = services.GetRequiredService<IOperationCompletionNotifier>();
        }

        [CommandHandler(Priority = 1000, IsFilter = true)]
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

            var tScope = typeof(DbOperationScope<TDbContext>);
            if (context.Items[tScope] != null) // Safety check
                throw Stl.Internal.Errors.InternalError($"'{tScope}' scope is already provided. Duplicate handler?");

            await using var scope = Services.GetRequiredService<DbOperationScope<TDbContext>>();
            var operation = scope.Operation;
            operation.Command = command;
            context.Items.Set(scope);

            var logEnabled = LogLevel != LogLevel.None && Log.IsEnabled(LogLevel);
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
                try {
                    await scope.RollbackAsync();
                }
                catch {
                    // Intended
                }
                throw;
            }
            if (scope.IsUsed) {
                if (logEnabled)
                    Log.Log(LogLevel, "Operation succeeded: {Command}", command);
                var completion = Completion.New(operation);
                context.Items.Set(completion);
                OperationCompletionNotifier.NotifyCompleted(operation);
                await context.Commander.RunAsync(completion, true, default).ConfigureAwait(false);
            }
        }
    }
}
