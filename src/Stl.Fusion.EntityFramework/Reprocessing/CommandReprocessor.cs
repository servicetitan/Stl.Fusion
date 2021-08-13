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
            public Func<CommandReprocessingState> ReprocessingStateFactory { get; init; } =
                () => new CommandReprocessingState();
            public Func<CommandReprocessingState, CommandReprocessingDecision> DecisionFactory { get; init; } =
                state => state.ComputeDecision();
            public IMomentClock? DelayClock { get; init; }
        }

        public Func<CommandReprocessingState> ReprocessingStateFactory { get; init; }
        public Func<CommandReprocessingState, CommandReprocessingDecision> DecisionFactory { get; init; }
        protected IMomentClock DelayClock { get; init; }
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
            ReprocessingStateFactory = options.ReprocessingStateFactory;
            DecisionFactory = options.DecisionFactory;
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
                    reprocessingState = reprocessingState with {
                        FailureCount = reprocessingState.FailureCount + 1,
                        Error = error,
                    };
                    var decision = DecisionFactory(reprocessingState);
                    if (!decision.ShouldReprocess)
                        throw;
                    reprocessingState = reprocessingState with { Decision = decision };
                    Log.LogWarning(
                        "Reprocessing: {Command}, state = {ReprocessingState}",
                        command, reprocessingState);
                    await DelayClock.Delay(decision.ReprocessingDelay, cancellationToken).ConfigureAwait(false);
                    context.ExecutionState = executionStateBackup;
                    context.Items.Items = itemsBackup;
                }
            }
        }
    }
}
