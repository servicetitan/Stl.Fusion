using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Stl.CommandR.Configuration;
using Stl.EntityFramework;

namespace Stl.CommandR.Handlers
{
    public class DbWriterHandler<TDbContext> : DbServiceBase<TDbContext>, ICommandHandler<IDbWriter<TDbContext>>
        where TDbContext : DbContext
    {
        public DbWriterHandler(IServiceProvider services) : base(services) { }

        [CommandHandler(Order = -1000)]
        public virtual async Task OnCommandAsync(IDbWriter<TDbContext> command, CommandContext context, CancellationToken cancellationToken)
        {
            var dbContextOpt = context.Items.TryGet<TDbContext>();
            if (dbContextOpt.HasValue) {
                await context.InvokeNextHandlerAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            await Tx.ReadWriteAsync(command, async dbContext => {
                context.Items.Set(dbContext);
                await context.InvokeNextHandlerAsync(cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
