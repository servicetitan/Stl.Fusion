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
        public class Options
        {
            public LogLevel LogLevel { get; set; } = LogLevel.None;
        }

        protected IOperationCompletionNotifier OperationCompletionNotifier { get; }
        protected IMomentClock Clock { get; }
        protected IServiceProvider Services { get; }
        protected LogLevel LogLevel { get; }
        protected ILogger Log { get; }

        public TransientOperationScopeProvider(
            Options? options,
            IServiceProvider services,
            ILogger<InvalidateOnCompletionCommandHandler>? log = null)
        {
            options ??= new();
            Log = log ?? NullLogger<InvalidateOnCompletionCommandHandler>.Instance;
            LogLevel = options.LogLevel;
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
                await scope.RollbackAsync();
                throw;
            }

            // Since this is the outermost scope handler, it's reasonable to run the
            // ICompletion command right from it for any other scope too.
            var completion = context.Items.TryGet<ICompletion>();
            if (completion == null) { // Also means scope.IsUsed == true
                if (logEnabled)
                    Log.Log(LogLevel, "Operation succeeded: {Command}", command);
                completion = Completion.New(operation);
                OperationCompletionNotifier.NotifyCompleted(operation);
                try {
                    await context.Commander.CallAsync(completion, true, default).ConfigureAwait(false);
                }
                catch (Exception e) {
                    Log.LogError(e, "Local operation completion failed! Command: {Command}", command);
                    // No throw: the operation itself succeeded
                }
            }
        }
    }
}
