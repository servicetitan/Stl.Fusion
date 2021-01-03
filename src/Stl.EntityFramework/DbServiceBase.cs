using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Time;

namespace Stl.EntityFramework
{
    public abstract class DbServiceBase<TDbContext>
        where TDbContext : DbContext
    {
        protected IServiceProvider Services { get; }
        protected IDbContextFactory<TDbContext> DbContextFactory { get; }
        protected IDbTransactionRunner<TDbContext> Tx { get; }
        protected IMomentClock Clock { get; }
        protected ILogger Log { get; }

        protected DbServiceBase(IServiceProvider services)
        {
            Services = services;
            DbContextFactory = services.GetRequiredService<IDbContextFactory<TDbContext>>();
            Tx = services.GetRequiredService<IDbTransactionRunner<TDbContext>>();
            Clock = services.GetService<IMomentClock>() ?? SystemClock.Instance;
            var loggerFactory = services.GetService<ILoggerFactory>();
            Log = loggerFactory?.CreateLogger(GetType()) ?? NullLogger.Instance;
        }

        protected virtual TDbContext CreateDbContext(DbAccessMode accessMode = DbAccessMode.ReadOnly)
        {
            var dbContext = DbContextFactory.CreateDbContext();
            dbContext.SetAccessMode(accessMode);
            return dbContext;
        }
    }
}
