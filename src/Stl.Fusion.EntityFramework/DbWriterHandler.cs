using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Stl.CommandR;
using Stl.CommandR.Configuration;

namespace Stl.Fusion.EntityFramework
{
    public class DbWriterHandler<TDbContext> : DbServiceBase<TDbContext>, ICommandHandler<IDbWriter<TDbContext>>
        where TDbContext : DbContext
    {
        public DbWriterHandler(IServiceProvider services) : base(services) { }

        [CommandHandler(Order = -1000, IsFilter = true)]
        public async Task OnCommandAsync(IDbWriter<TDbContext> command, CommandContext context, CancellationToken cancellationToken)
        {
            var dbContextOpt = context.Items.TryGet<TDbContext>();
            if (dbContextOpt.HasValue) {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            await Tx.ReadWriteAsync(command, async dbContext => {
                context.Items.Set(dbContext);
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
