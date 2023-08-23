using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Npgsql.Operations;

public class NpgsqlDbOperationLogChangeNotifier<TDbContext>(
    NpgsqlDbOperationLogChangeTrackingOptions<TDbContext> options,
    IServiceProvider services
    ) : DbOperationCompletionNotifierBase<TDbContext, NpgsqlDbOperationLogChangeTrackingOptions<TDbContext>>(options, services)
    where TDbContext : DbContext
{
    // Protected methods

    protected override async Task Notify(Tenant tenant)
    {
        var dbContext = CreateDbContext(tenant);
        await using var _ = dbContext.ConfigureAwait(false);

#pragma warning disable MA0074
        var qPayload = AgentInfo.Id.Value.Replace("'", "''");
#pragma warning restore MA0074
        await dbContext.Database
            .ExecuteSqlRawAsync($"NOTIFY {Options.ChannelName}, '{qPayload}'")
            .ConfigureAwait(false);
    }
}
