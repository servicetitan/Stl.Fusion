using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.CommandR.Filters
{
    public static class CommandRBuilderEx
    {
        public static CommandRBuilder AddDbWriterFilter<TDbContext>(this CommandRBuilder commandR, double? priorityOverride = null)
            where TDbContext : DbContext
        {
            commandR.Services.AddSingleton<DbWriterFilter<TDbContext>>();
            return commandR.AddHandlers<DbWriterFilter<TDbContext>>(priorityOverride);
        }
    }
}
