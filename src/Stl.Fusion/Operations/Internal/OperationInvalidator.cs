using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.CommandR;

namespace Stl.Fusion.Operations.Internal
{
    public class OperationInvalidator : IOperationCompletionListener
    {
        protected ICommander Commander { get; }
        protected AgentInfo AgentInfo { get; }
        protected IInvalidationInfoProvider InvalidationInfoProvider { get; }
        protected ILogger<OperationInvalidator> Log { get; }

        public OperationInvalidator(
            ICommander commander,
            AgentInfo agentInfo,
            IInvalidationInfoProvider invalidationInfoProvider,
            ILogger<OperationInvalidator>? log = null)
        {
            Log = log ?? NullLogger<OperationInvalidator>.Instance;
            AgentInfo = agentInfo;
            InvalidationInfoProvider = invalidationInfoProvider;
            Commander = commander;
        }

        public virtual void OnOperationCompleted(IOperation operation)
        {
            if (operation.AgentId == AgentInfo.Id.Value)
                // Local operations are invalidated by InvalidationHandler
                return;
            if (!(operation.Command is ICommand command))
                return;
            if (!InvalidationInfoProvider.RequiresInvalidation(command))
                return;
            if (Log.IsEnabled(LogLevel.Debug))
                Log.LogDebug("Invalidating operation: agent {0}, command {1}", operation.AgentId, command);
            Commander.Start(InvalidateCommand.New(command, operation), true);
        }
    }
}
