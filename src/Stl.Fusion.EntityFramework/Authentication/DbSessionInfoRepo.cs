using Microsoft.EntityFrameworkCore;
using Stl.Fusion.Authentication;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Authentication;

public interface IDbSessionInfoRepo<in TDbContext, TDbSessionInfo, in TDbUserId>
    where TDbContext : DbContext
    where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
    where TDbUserId : notnull
{
    Type SessionInfoEntityType { get; }

    // Write methods
    Task<TDbSessionInfo> GetOrCreate(
        TDbContext dbContext, string sessionId, CancellationToken cancellationToken = default);
    Task<TDbSessionInfo> Upsert(
        TDbContext dbContext, string sessionId, SessionInfo sessionInfo, CancellationToken cancellationToken = default);
    Task<int> Trim(
        Tenant tenant, DateTime minLastSeenAt, int maxCount, CancellationToken cancellationToken = default);

    // Read methods
    Task<TDbSessionInfo?> Get(
        Tenant tenant, string sessionId, CancellationToken cancellationToken = default);
    Task<TDbSessionInfo?> Get(
        TDbContext dbContext, string sessionId, bool forUpdate, CancellationToken cancellationToken = default);
    Task<TDbSessionInfo[]> ListByUser(
        TDbContext dbContext, TDbUserId userId, CancellationToken cancellationToken = default);
}

public class DbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId> : DbServiceBase<TDbContext>,
    IDbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId>
    where TDbContext : DbContext
    where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
    where TDbUserId : notnull
{
    protected DbAuthService<TDbContext>.Options Options { get; }
    protected IDbUserIdHandler<TDbUserId> DbUserIdHandler { get; init; }
    protected IDbEntityResolver<string, TDbSessionInfo> SessionResolver { get; init; }
    protected IDbEntityConverter<TDbSessionInfo, SessionInfo> SessionConverter { get; init; }
    protected ITenantResolver<TDbContext> TenantResolver { get; init; }

    public Type SessionInfoEntityType => typeof(TDbSessionInfo);

    public DbSessionInfoRepo(DbAuthService<TDbContext>.Options options, IServiceProvider services)
        : base(services)
    {
        Options = options;
        DbUserIdHandler = services.GetRequiredService<IDbUserIdHandler<TDbUserId>>();
        SessionResolver = services.DbEntityResolver<string, TDbSessionInfo>();
        SessionConverter = services.DbEntityConverter<TDbSessionInfo, SessionInfo>();
        TenantResolver = services.GetRequiredService<ITenantResolver<TDbContext>>();
    }

    // Write methods

    public virtual async Task<TDbSessionInfo> GetOrCreate(
        TDbContext dbContext, string sessionId, CancellationToken cancellationToken = default)
    {
        var dbSessionInfo = await Get(dbContext, sessionId, true, cancellationToken).ConfigureAwait(false);
        if (dbSessionInfo == null) {
            var session = new Session(sessionId);
            var sessionInfo = new SessionInfo(session, Clocks.SystemClock.Now);
            dbSessionInfo = dbContext.Add(
                new TDbSessionInfo() {
                    Id = sessionId,
                    CreatedAt = sessionInfo.CreatedAt,
                }).Entity;
            SessionConverter.UpdateEntity(sessionInfo, dbSessionInfo);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        return dbSessionInfo;
    }

    public async Task<TDbSessionInfo> Upsert(
        TDbContext dbContext, string sessionId, SessionInfo sessionInfo, CancellationToken cancellationToken = default)
    {
        var dbSessionInfo = await dbContext.Set<TDbSessionInfo>()
            .FindAsync(DbKey.Compose(sessionId), cancellationToken)
            .ConfigureAwait(false);
        var isDbSessionInfoFound = dbSessionInfo != null;
        dbSessionInfo ??= new TDbSessionInfo() {
            Id = sessionId,
            CreatedAt = sessionInfo.CreatedAt,
        };
        SessionConverter.UpdateEntity(sessionInfo, dbSessionInfo);
        if (isDbSessionInfoFound)
            dbContext.Update(dbSessionInfo);
        else
            dbContext.Add(dbSessionInfo);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return dbSessionInfo;
    }

    public virtual async Task<int> Trim(
        Tenant tenant, DateTime minLastSeenAt, int maxCount, CancellationToken cancellationToken = default)
    {
        var dbContext = CreateDbContext(tenant, true);
        await using var _ = dbContext.ConfigureAwait(false);
        dbContext.DisableChangeTracking();

        var entities = await dbContext.Set<TDbSessionInfo>().AsQueryable()
            .Where(o => o.LastSeenAt < minLastSeenAt)
            .OrderBy(o => o.LastSeenAt)
            .Take(maxCount)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (entities.Count == 0)
            return 0;
        foreach (var e in entities)
            dbContext.Remove(e);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entities.Count;
    }

    // Read methods

    public async Task<TDbSessionInfo?> Get(Tenant tenant, string sessionId, CancellationToken cancellationToken = default) 
        => await SessionResolver.Get(tenant, sessionId, cancellationToken).ConfigureAwait(false);

    public virtual async Task<TDbSessionInfo?> Get(
        TDbContext dbContext, string sessionId, bool forUpdate, CancellationToken cancellationToken = default)
    {
        var dbSessionInfos = forUpdate ? dbContext.Set<TDbSessionInfo>().ForUpdate() : dbContext.Set<TDbSessionInfo>();
        return await dbSessionInfos
            .SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual async Task<TDbSessionInfo[]> ListByUser(
        TDbContext dbContext, TDbUserId userId, CancellationToken cancellationToken = default)
    {
        var qSessions =
            from s in dbContext.Set<TDbSessionInfo>().AsQueryable()
            where Equals(s.UserId, userId)
            orderby s.LastSeenAt descending
            select s;
        var sessions = (TDbSessionInfo[]) await qSessions.ToArrayAsync(cancellationToken).ConfigureAwait(false);
        return sessions;
    }
}
