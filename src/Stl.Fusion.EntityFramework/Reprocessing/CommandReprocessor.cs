using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.CommandR;
using Stl.CommandR.Commands;
using Stl.CommandR.Configuration;
using Stl.Generators;
using Stl.Time;

namespace Stl.Fusion.EntityFramework.Reprocessing
{
    /// <summary>
    /// Tries to reprocess commands that failed with <see cref="DbUpdateException"/>
    /// </summary>
    public class CommandReprocessor : ICommandHandler<ICommand>
    {
        public class Options
        {
            public int MaxAttemptCount { get; init; } = 3;
            public Func<CommandReprocessingState, bool>? ShouldReprocessFunc { get; init; }
            public Func<CommandReprocessingState, TimeSpan>? GetReprocessingDelayFunc { get; init; }
            public Func<CommandReprocessingState>? ReprocessingStateFactory { get; init; }
            public IMomentClock? DelayClock { get; init; }
        }

        protected int MaxAttemptCount { get; init; }
        protected Func<CommandReprocessingState, bool> ShouldReprocessFunc { get; init; }
        protected Func<CommandReprocessingState, TimeSpan> GetReprocessingDelayFunc { get; init; }
        protected Func<CommandReprocessingState> ReprocessingStateFactory { get; init; }
        protected IMomentClock DelayClock { get; init; }
        protected Generator<long> Rng { get; init; } = new RandomInt64Generator();
        protected IServiceProvider Services { get; init; }
        protected ILogger Log { get; init; }

        public CommandReprocessor(
            Options? options,
            IServiceProvider services,
            ILogger<CommandReprocessor>? log = null)
        {
            options ??= new();
            Log = log ?? NullLogger<CommandReprocessor>.Instance;
            Services = services;
            MaxAttemptCount = options.MaxAttemptCount;
            ShouldReprocessFunc = options.ShouldReprocessFunc ?? ShouldReprocess;
            GetReprocessingDelayFunc = options.GetReprocessingDelayFunc ?? GetReprocessingDelay;
            ReprocessingStateFactory = options.ReprocessingStateFactory ?? (() => new CommandReprocessingState());
            DelayClock = options.DelayClock ?? services.Clocks().CpuClock;
        }

        [CommandHandler(Priority = 100_000, IsFilter = true)]
        public virtual async Task OnCommand(ICommand command, CommandContext context, CancellationToken cancellationToken)
        {
            var reprocessingAllowed =
                context.OuterContext == null // Should be a top-level command
                && !(command is IMetaCommand) // No reprocessing for meta commands
                && !Computed.IsInvalidating();
            if (!reprocessingAllowed) {
                await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
                return;
            }

            var executionStateBackup = context.ExecutionState;
            var itemsBackup = context.Items.Items;
            var reprocessingState = ReprocessingStateFactory.Invoke();
            while (true) {
                context.Items.Set(reprocessingState);
                try {
                    await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception? error) {
                    reprocessingState = context.Items.Get<CommandReprocessingState>();
                    reprocessingState = reprocessingState.Next(error);
                    if (!ShouldReprocessFunc.Invoke(reprocessingState))
                        throw;
                    var delay = GetReprocessingDelayFunc.Invoke(reprocessingState);
                    Log.LogWarning(error, "Reprocessing: {Command} with delay = {Delay}", command, delay);
                    await DelayClock.Delay(delay, cancellationToken).ConfigureAwait(false);
                    context.ExecutionState = executionStateBackup;
                    context.Items.Items = itemsBackup;
                }
            }
        }

        // Protected methods

        protected virtual bool ShouldReprocess(CommandReprocessingState reprocessingState)
        {
            if (reprocessingState.FailureCount >= MaxAttemptCount)
                return false;
            return reprocessingState.LastError is DbUpdateException;
        }

        protected virtual TimeSpan GetReprocessingDelay(CommandReprocessingState reprocessingState)
            => TimeSpan.FromMilliseconds(10 + Math.Abs(Rng.Next() % 100));
    }
}
