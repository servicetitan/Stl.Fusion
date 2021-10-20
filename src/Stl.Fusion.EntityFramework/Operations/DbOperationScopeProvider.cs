using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Operations.Reprocessing;

namespace Stl.Fusion.EntityFramework.Operations;

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

        await using var scope = Services.GetRequiredService<DbOperationScope<TDbContext>>();
        var operation = scope.Operation;
        operation.Command = command;
        context.Items.Set(scope);

        try {
            await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
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
            var operationReprocessor = context.Items.TryGet<IOperationReprocessor>();
            if (operationReprocessor == null)
                throw;

            // 3. Check if it's a transient failure
            var executionStrategy = scope.MasterDbContext?.Database.CreateExecutionStrategy();
            if (!IsTransientFailure(error, executionStrategy))
                throw;

            // 4. "Tag" error as transient in operation reprocessor
            operationReprocessor.AddTransientFailure(error);

            // 5. Log "Operation failed" if it's our last retry
            if (!operationReprocessor.WillRetry(error))
                Log.LogError(error, "Operation failed: {Command}", command);

            throw;
        }
    }

    protected virtual bool IsTransientFailure(Exception error, IExecutionStrategy? executionStrategy)
    {
        if (executionStrategy is not { RetriesOnFailure: false })
            return false; // Can't detect failures w/ such execution strategy

        try {
            executionStrategy.Execute(error, state => throw state); // Simply re-throwing an error
            return false; // We should never land here
        }
        catch (Exception caught) {
            // Default failure detectors throw InvalidOperationException
            // if they see the exception they catch can be reprocessed
            return caught != error;
        }
    }
}
