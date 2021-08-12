using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.EntityFramework.Authentication
{
    public interface IDbSessionInfoRepo<in TDbContext, TDbSessionInfo, TDbUserId>
        where TDbContext : DbContext
        where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
        where TDbUserId : notnull
    {
        Type SessionInfoEntityType { get; }

        // Write methods
        Task<TDbSessionInfo> GetOrCreate(
            TDbContext dbContext, string sessionId, CancellationToken cancellationToken = default);
        Task<TDbSessionInfo> CreateOrUpdate(
            TDbContext dbContext, SessionInfo sessionInfo, CancellationToken cancellationToken = default);
        Task<int> Trim(
            DateTime minLastSeenAt, int maxCount, CancellationToken cancellationToken = default);

        // Read methods
        Task<TDbSessionInfo?> TryGet(string sessionId, CancellationToken cancellationToken = default);
        Task<TDbSessionInfo?> TryGet(
            TDbContext dbContext, string sessionId, CancellationToken cancellationToken = default);
        Task<TDbSessionInfo[]> ListByUser(
            TDbContext dbContext, TDbUserId userId, CancellationToken cancellationToken = default);
    }

    public class DbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId>
        : DbServiceBase<TDbContext>, IDbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId>
        where TDbContext : DbContext
        where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
        where TDbUserId : notnull
    {
        protected DbAuthService<TDbContext>.Options Options { get; }
        protected DbEntityResolver<TDbContext, string, TDbSessionInfo> EntityResolver { get; }
        protected IDbUserIdHandler<TDbUserId> DbUserIdHandler { get; }

        public Type SessionInfoEntityType => typeof(TDbSessionInfo);

        public DbSessionInfoRepo(DbAuthService<TDbContext>.Options options, IServiceProvider services)
            : base(services)
        {
            Options = options;
            EntityResolver = services.GetRequiredService<DbEntityResolver<TDbContext, string, TDbSessionInfo>>();
            DbUserIdHandler = services.GetRequiredService<IDbUserIdHandler<TDbUserId>>();
        }

        // Write methods

        public virtual async Task<TDbSessionInfo> GetOrCreate(
            TDbContext dbContext, string sessionId, CancellationToken cancellationToken = default)
        {
            var dbSessionInfo = await TryGet(dbContext, sessionId, cancellationToken).ConfigureAwait(false);
            if (dbSessionInfo == null) {
                var sessionInfo = new SessionInfo(sessionId, Clocks.SystemClock.Now);
                dbSessionInfo = dbContext.Add(
                    new TDbSessionInfo() {
                        Id = sessionInfo.Id,
                        CreatedAt = sessionInfo.CreatedAt,
                    }).Entity;
                dbSessionInfo.UpdateFrom(Services, sessionInfo);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            return dbSessionInfo;
        }

        public async Task<TDbSessionInfo> CreateOrUpdate(
            TDbContext dbContext, SessionInfo sessionInfo, CancellationToken cancellationToken = default)
        {
            var dbSessionInfo = await TryGet(dbContext, sessionInfo.Id, cancellationToken).ConfigureAwait(false);
            var isDbSessionInfoFound = dbSessionInfo != null;
            dbSessionInfo ??= new TDbSessionInfo() {
                Id = sessionInfo.Id,
                CreatedAt = sessionInfo.CreatedAt,
            };
            dbSessionInfo.UpdateFrom(Services, sessionInfo);
            if (isDbSessionInfoFound)
                dbContext.Update(dbSessionInfo);
            else
                dbContext.Add(dbSessionInfo);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return dbSessionInfo;
        }

        public virtual async Task<int> Trim(DateTime minLastSeenAt, int maxCount, CancellationToken cancellationToken = default)
        {
            await using var dbContext = CreateDbContext(true);
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

        public async Task<TDbSessionInfo?> TryGet(string sessionId, CancellationToken cancellationToken = default)
            => await EntityResolver.TryGet(sessionId, cancellationToken).ConfigureAwait(false);

        public virtual async Task<TDbSessionInfo?> TryGet(
            TDbContext dbContext, string sessionId, CancellationToken cancellationToken)
            => await dbContext.Set<TDbSessionInfo>()
                .FindAsync(ComposeKey(sessionId), cancellationToken)
                .ConfigureAwait(false);

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
}
