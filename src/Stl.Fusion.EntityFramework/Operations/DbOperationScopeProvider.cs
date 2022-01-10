using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Fusion.Operations.Reprocessing;
using Stl.Versioning;

namespace Stl.Fusion.EntityFramework.Operations;

public class DbOperationScopeProvider<TDbContext> : DbServiceBase<TDbContext>, ICommandHandler<ICommand>
    where TDbContext : DbContext
{
    protected static MemberInfo ExecutionStrategyShouldRetryOnMethod { get; } = typeof(ExecutionStrategy)
        .GetMethod("ShouldRetryOn", BindingFlags.Instance | BindingFlags.NonPublic)!;

    protected IOperationCompletionNotifier OperationCompletionNotifier { get; }

    public DbOperationScopeProvider(IServiceProvider services) : base(services)
        => OperationCompletionNotifier = services.GetRequiredService<IOperationCompletionNotifier>();

    [CommandHandler(Priority = 1000, IsFilter = true)]
    public async Task OnCommand(ICommand command, CommandContext context, CancellationToken cancellationToken)
    {
        var operationRequired =
            context.OuterContext == null // Should be a top-level command
            && !(command is IMetaCommand) // No operations for meta commands
            && !Computed.IsInvalidating();
        if (!operationRequired) {
            await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
            return;
        }

        var tScope = typeof(DbOperationScope<TDbContext>);
        if (context.Items[tScope] != null) // Safety check
            throw Stl.Internal.Errors.InternalError($"'{tScope}' scope is already provided. Duplicate handler?");

        var scope = Services.GetRequiredService<DbOperationScope<TDbContext>>();
        await using var _ = scope.ConfigureAwait(false);
        var operation = scope.Operation;
        operation.Command = command;
        context.Items.Set(scope);

        try {
            await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
            if (!scope.IsClosed)
                await scope.Commit(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception error) {
            // 1. Ensure everything is rolled back
            try {
                await scope.Rollback().ConfigureAwait(false);
            }
            catch {
                // Intended
            }

            // 2. Check if operation reprocessor is there
            var operationReprocessor = context.Items.Get<IOperationReprocessor>();
            if (operationReprocessor == null)
                throw;

            // 3. Check if it's a transient failure
            if (!IsTransientFailure(scope, error))
                throw;

            // 4. "Tag" error as transient in operation reprocessor
            operationReprocessor.AddTransientFailure(error);

            // 5. Log "Operation failed" if it's our last retry
            if (!operationReprocessor.WillRetry(error))
                Log.LogError(error, "Operation failed: {Command}", command);

            throw;
        }
    }

    protected virtual bool IsTransientFailure(
        DbOperationScope<TDbContext> scope,
        Exception error)
    {
        if (error is VersionMismatchException)
            return true;
        var executionStrategy = scope.MasterDbContext?.Database.CreateExecutionStrategy();
        if (executionStrategy is not ExecutionStrategy retryingExecutionStrategy)
            return false;
        var isTransient = retryingExecutionStrategy.ShouldRetryOn(error);
        return isTransient;
    }
}
