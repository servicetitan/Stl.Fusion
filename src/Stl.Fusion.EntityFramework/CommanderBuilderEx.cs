using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.CommandR;

namespace Stl.Fusion.EntityFramework
{
    public static class CommanderBuilderEx
    {
        public static CommanderBuilder AddDbWriterHandler<TDbContext>(this CommanderBuilder commander, double? priorityOverride = null)
            where TDbContext : DbContext
        {
            commander.Services.AddSingleton<DbWriterHandler<TDbContext>>();
            return commander.AddHandlers<DbWriterHandler<TDbContext>>(priorityOverride);
        }
    }
}
