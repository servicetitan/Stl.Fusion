using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.CommandR;

namespace Stl.Fusion.Operations.Internal
{
    public class OperationInvalidationHandler : IOperationCompletionHandler
    {
        protected ICommander Commander { get; }
        protected AgentInfo AgentInfo { get; }
        protected IInvalidationInfoProvider InvalidationInfoProvider { get; }
        protected ILogger<OperationInvalidationHandler> Log { get; }

        public OperationInvalidationHandler(
            ICommander commander,
            AgentInfo agentInfo,
            IInvalidationInfoProvider invalidationInfoProvider,
            ILogger<OperationInvalidationHandler>? log = null)
        {
            Log = log ?? NullLogger<OperationInvalidationHandler>.Instance;
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
            Commander.Start(Invalidate.New(command), true);
        }
    }
}
