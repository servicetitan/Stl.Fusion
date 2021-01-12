using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.CommandR;
using Stl.CommandR.Configuration;

namespace Stl.Fusion.EntityFramework
{
    public class DbTransactionScopeHandler<TDbContext> : DbServiceBase<TDbContext>, ICommandHandler<ICommand>
        where TDbContext : DbContext
    {
        public DbTransactionScopeHandler(IServiceProvider services) : base(services) { }

        [CommandHandler(Order = -1000, IsFilter = true)]
        public async Task OnCommandAsync(ICommand command, CommandContext context, CancellationToken cancellationToken)
        {
            var existingTransaction = context.Items.TryGet<IDbTransactionScope<TDbContext>>();
            if (existingTransaction != null) {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            await using var transaction = Services.GetRequiredService<IDbTransactionScope<TDbContext>>();
            context.Items.Set(transaction);
            await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(command, cancellationToken);
        }
    }
}
