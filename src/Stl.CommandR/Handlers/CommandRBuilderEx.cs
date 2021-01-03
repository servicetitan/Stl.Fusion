using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.CommandR.Handlers
{
    public static class CommandRBuilderEx
    {
        public static CommandRBuilder AddDbWriterFilter<TDbContext>(this CommandRBuilder commandR, double? priorityOverride = null)
            where TDbContext : DbContext
        {
            commandR.Services.AddSingleton<DbWriterHandler<TDbContext>>();
            return commandR.AddHandlers<DbWriterHandler<TDbContext>>(priorityOverride);
        }
    }
}
