using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.CommandR;
using Stl.Time;

namespace Stl.Fusion.EntityFramework
{
    public abstract class DbAsyncProcessBase<TDbContext> : AsyncProcessBase
        where TDbContext : DbContext
    {
        protected IServiceProvider Services { get; }
        protected IDbContextFactory<TDbContext> DbContextFactory { get; }
        protected IMomentClock Clock { get; }
        protected ILogger Log { get; }

        protected DbAsyncProcessBase(IServiceProvider services)
        {
            Services = services;
            DbContextFactory = services.GetRequiredService<IDbContextFactory<TDbContext>>();
            Clock = services.GetService<IMomentClock>() ?? SystemClock.Instance;
            var loggerFactory = services.GetService<ILoggerFactory>();
            Log = loggerFactory?.CreateLogger(GetType()) ?? NullLogger.Instance;
        }

        protected TDbContext CreateDbContext()
            => DbContextFactory.CreateDbContext().ReadWrite(false);

        protected Task<TDbContext> CreateCommandDbContextAsync(CancellationToken cancellationToken = default)
        {
            var commandContext = CommandContext.GetCurrent();
            var operationScope = commandContext.Items.Get<IDbOperationScope<TDbContext>>();
            return operationScope.GetDbContextAsync(cancellationToken);
        }

        protected IDbOperationScope<TDbContext> BeginOperation()
            => Services.GetRequiredService<IDbOperationScope<TDbContext>>();
    }
}
