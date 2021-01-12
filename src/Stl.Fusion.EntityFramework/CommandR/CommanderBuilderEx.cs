using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.CommandR;
using Stl.Fusion.EntityFramework.CommandR.Internal;

namespace Stl.Fusion.EntityFramework.CommandR
{
    public static class CommanderBuilderEx
    {
        public static CommanderBuilder AddTransactionScopeHandler<TDbContext>(this CommanderBuilder commander, double? priorityOverride = null)
            where TDbContext : DbContext
        {
            commander.Services.AddDbContextServices<TDbContext>().AddTransactionScope();
            commander.Services.AddSingleton<DbTransactionScopeHandler<TDbContext>>();
            return commander.AddHandlers<DbTransactionScopeHandler<TDbContext>>(priorityOverride);
        }
    }
}
