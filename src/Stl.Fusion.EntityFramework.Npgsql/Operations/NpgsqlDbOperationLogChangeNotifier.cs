using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Locking;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Npgsql.Operations;

public class NpgsqlDbOperationLogChangeNotifier<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbContext>
    (NpgsqlDbOperationLogChangeTrackingOptions<TDbContext> options, IServiceProvider services)
    : DbOperationCompletionNotifierBase<TDbContext, NpgsqlDbOperationLogChangeTrackingOptions<TDbContext>>(options, services)
    where TDbContext : DbContext
{
    private readonly ConcurrentDictionary<Symbol, CachedInfo> _cache = new();

    // Protected methods

    protected override async Task Notify(Tenant tenant)
    {
        var info = GetCachedInfo(tenant);
        var (dbContext, sql, asyncLock) = info;
        using var _ = await asyncLock.Lock().ConfigureAwait(false);
        using var cts = new CancellationTokenSource(1000);
        try {
            await dbContext.Database.ExecuteSqlRawAsync(sql, cts.Token).ConfigureAwait(false);
        }
        catch {
            // Dispose dbContext & remove cached info to make sure it gets recreated
            try {
                await dbContext.DisposeAsync().ConfigureAwait(false);
            }
            catch {
                // Intended
            }
            _cache.TryRemove(tenant.Id, info);
        }
    }

    private CachedInfo GetCachedInfo(Tenant tenant)
        => _cache.GetOrAdd(tenant.Id, static (_, x) => x.self.CreateCachedInfo(x.tenant), (tenant, self: this));

    private CachedInfo CreateCachedInfo(Tenant tenant)
    {
        var dbContext = CreateDbContext(tenant);
        var quotedPayload = AgentInfo.Id.Value
#if NETSTANDARD2_0
            .Replace("'", "''");
#else
            .Replace("'", "''", StringComparison.Ordinal);
#endif
        var sql = $"NOTIFY {Options.ChannelName}, '{quotedPayload}'";
        return new CachedInfo(dbContext, sql, new SimpleAsyncLock());
    }

    // Nested types

    private sealed record CachedInfo(TDbContext DbContext, string Sql, SimpleAsyncLock AsyncLock);
}
