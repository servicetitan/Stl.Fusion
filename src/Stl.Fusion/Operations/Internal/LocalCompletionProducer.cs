using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.CommandR;
using Stl.CommandR.Commands;
using Stl.CommandR.Configuration;
using Stl.Time;

namespace Stl.Fusion.Operations.Internal
{
    public class LocalCompletionProducer : ICommandHandler<ICommand>
    {
        public class Options
        {
            public LogLevel LogLevel { get; set; } = LogLevel.None;
        }

        private long _surrogateOperationId;
        protected AgentInfo AgentInfo { get; }
        protected IMomentClock Clock { get; }
        protected LogLevel LogLevel { get; }
        protected ILogger Log { get; }

        public LocalCompletionProducer(Options? options,
            AgentInfo agentInfo,
            IInvalidationInfoProvider invalidationInfoProvider,
            IMomentClock? clock = null,
            ILogger<InvalidateOnCompletionCommandHandler>? log = null)
        {
            options ??= new();
            Log = log ?? NullLogger<InvalidateOnCompletionCommandHandler>.Instance;
            LogLevel = options.LogLevel;
            AgentInfo = agentInfo;
            Clock = clock ?? SystemClock.Instance;
        }

        [CommandHandler(Priority = 10_000, IsFilter = true)]
        public async Task OnCommandAsync(ICommand command, CommandContext context, CancellationToken cancellationToken)
        {
            var startTime = Clock.Now;
            await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);

            var requiresCompletion =
                context.OuterContext == null // Should be a top-level command
                && !(command is IMetaCommand) // Shouldn't be a "second-order" command
                && !Computed.IsInvalidating(); // Invalidating = definitely not a real operation
            if (!requiresCompletion)
                return;

            var completion = context.Items.TryGet<ICompletion>();
            if (completion == null) {
                // DbOperationScopeHandler wasn't used, so we have to create
                // an EphemeralOperation describing the command that succeeded,
                // but wasn't committed to any DbOperationScope
                var id = Interlocked.Increment(ref _surrogateOperationId);
                var operation = new EphemeralOperation() {
                    Id = $"Local-{id}",
                    AgentId = AgentInfo.Id.Value,
                    Command = command,
                    StartTime = startTime,
                    CommitTime = Clock.Now,
                };
                operation.CaptureItems(context.Items);
                completion = Completion.New(operation);
            }
            await context.Commander.RunAsync(completion, true, default).ConfigureAwait(false);
        }
    }
}
