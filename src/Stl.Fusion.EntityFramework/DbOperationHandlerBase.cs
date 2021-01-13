using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion.EntityFramework
{
    public abstract class DbOperationHandlerBase<TDbContext> : DbServiceBase<TDbContext>, IDisposable
        where TDbContext : DbContext
    {
        protected IDbOperationNotifier<TDbContext> DbOperationNotifier { get; }

        protected DbOperationHandlerBase(IServiceProvider services) : base(services)
        {
            DbOperationNotifier = services.GetRequiredService<IDbOperationNotifier<TDbContext>>();
            DbOperationNotifier.ConfirmedOperation += OnOperation;
        }

        protected virtual void Dispose(bool disposing)
        {
            DbOperationNotifier.ConfirmedOperation -= OnOperation;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void OnOperation(IDbOperation dbOperation);
    }
}
