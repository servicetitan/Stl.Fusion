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
        public class Options
        {
            public LogLevel LogLevel { get; set; } = LogLevel.None;
        }

        protected IOperationCompletionNotifier? OperationCompletionNotifier { get; }
        protected IInvalidationInfoProvider? InvalidationInfoProvider { get; }
        protected LogLevel LogLevel { get; }

        public DbOperationScopeHandler(
            Options? options,
            IServiceProvider services)
            : base(services)
        {
            options ??= new();
            LogLevel = options.LogLevel;
            OperationCompletionNotifier = services.GetService<IOperationCompletionNotifier>();
            InvalidationInfoProvider = services.GetService<IInvalidationInfoProvider>();
        }

        [CommandHandler(Priority = 1000, IsFilter = true)]
        public async Task OnCommandAsync(ICommand command, CommandContext context, CancellationToken cancellationToken)
        {
            var skip = context.OuterContext != null // Should be top-level command
                || command is ICompletion // Second handler here will take care of it
                || Computed.IsInvalidating();
            if (skip) {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            var tScope = typeof(IDbOperationScope<TDbContext>);
            if (context.Items[tScope] != null) // Safety check
                throw Stl.Internal.Errors.InternalError($"'{tScope}' scope is already provided. Duplicate handler?");

            var logEnabled = LogLevel != LogLevel.None && Log.IsEnabled(LogLevel);
            await using var scope = Services.GetRequiredService<IDbOperationScope<TDbContext>>();
            scope.Command = command;
            context.Items.Set(scope);
            if (logEnabled)
                Log.Log(LogLevel, "+ Operation started: {0}", command);

            IOperation? operation = null;
            try {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);

                // Building IOperation.Items from CommandContext.Items
                foreach (var (key, value) in context.Items.Items) {
                    if (value is IOperationItem)
                        scope.Items = scope.Items.Set(key, value);
                }
                operation = await scope.CommitAsync(cancellationToken);
                if (logEnabled)
                    Log.Log(LogLevel, "- Operation succeeded: {0}", command);
            }
            catch (OperationCanceledException) {
                throw;
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
            if (operation != null) {
                if (InvalidationInfoProvider?.RequiresInvalidation(command) ?? false)
                    context.Items.Set(Completion.New(command, operation));
                OperationCompletionNotifier?.NotifyCompleted(operation);
            }
        }
    }
}
