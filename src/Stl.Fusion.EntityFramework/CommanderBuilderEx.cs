using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.CommandR;

namespace Stl.Fusion.EntityFramework
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
