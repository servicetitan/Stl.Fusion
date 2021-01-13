using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.CommandR;
using Stl.Fusion.EntityFramework.CommandR.Internal;

namespace Stl.Fusion.EntityFramework.CommandR
{
    public static class CommanderBuilderEx
    {
        public static CommanderBuilder AddTransactionScopeHandler<TDbContext>(this CommanderBuilder commander, double? orderOverride = null)
            where TDbContext : DbContext
        {
            commander.Services.AddDbContextServices<TDbContext>().AddOperations();
            commander.Services.AddSingleton<DbOperationScopeHandler<TDbContext>>();
            return commander.AddHandlers<DbOperationScopeHandler<TDbContext>>(orderOverride);
        }
    }
}
