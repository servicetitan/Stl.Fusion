using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.CommandR;
using Stl.Fusion.CommandR;

namespace Stl.Fusion.EntityFramework
{
    public class DbOperationInvalidationHandler<TDbContext> : DbOperationHandlerBase<TDbContext>
        where TDbContext : DbContext
    {
        protected AgentInfo AgentInfo { get; }
        protected IInvalidationInfoProvider InvalidationInfoProvider { get; }
        protected ICommander Commander { get; }

        public DbOperationInvalidationHandler(IServiceProvider services)
            : base(services)
        {
            AgentInfo = services.GetRequiredService<AgentInfo>();
            InvalidationInfoProvider = services.GetRequiredService<IInvalidationInfoProvider>();
            Commander = services.Commander();
        }

        protected override void OnOperation(IDbOperation dbOperation)
        {
            if (dbOperation.AgentId == AgentInfo.Id.Value)
                // Local operations are invalidated by InvalidationHandler
                return;
            if (!(dbOperation.Operation is ICommand command))
                return;
            if (!InvalidationInfoProvider.RequiresInvalidation(command))
                return;
            if (Log.IsEnabled(LogLevel.Debug))
                Log.LogDebug("Invalidating operation: agent {0}, command {1}", dbOperation.AgentId, command);
            Commander.Start(Invalidate.New(command), true);
        }
    }
}
