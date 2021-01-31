using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Fusion.EntityFramework.Internal;

namespace Stl.Fusion.EntityFramework.Authentication
{
    public interface IDbSessionInfoRepo<in TDbContext>
        where TDbContext : DbContext
    {
        Type SessionInfoEntityType { get; }

        // Write methods
        Task<DbSessionInfo> FindOrCreateAsync(
            TDbContext dbContext, string sessionId, CancellationToken cancellationToken = default);
        Task<DbSessionInfo> CreateOrUpdateAsync(
            TDbContext dbContext, SessionInfo sessionInfo, CancellationToken cancellationToken = default);
        Task<int> TrimAsync(
            DateTime minLastSeenAt, int maxCount, CancellationToken cancellationToken = default);

        // Read methods
        Task<DbSessionInfo?> FindAsync(string sessionId, CancellationToken cancellationToken = default);
        Task<DbSessionInfo?> FindAsync(
            TDbContext dbContext, string sessionId, CancellationToken cancellationToken = default);
        Task<DbSessionInfo[]> ListByUserAsync(
            TDbContext dbContext, long userId, CancellationToken cancellationToken = default);
    }

    public class DbSessionInfoRepo<TDbContext, TDbSessionInfo> : DbServiceBase<TDbContext>, IDbSessionInfoRepo<TDbContext>
        where TDbContext : DbContext
        where TDbSessionInfo : DbSessionInfo, new()
    {
        protected DbAuthService<TDbContext>.Options Options { get; }
        protected DbEntityResolver<TDbContext, string, TDbSessionInfo> EntityResolver { get; }

        public Type SessionInfoEntityType => typeof(TDbSessionInfo);

        public DbSessionInfoRepo(DbAuthService<TDbContext>.Options options, IServiceProvider services)
            : base(services)
        {
            Options = options;
            EntityResolver = services.GetRequiredService<DbEntityResolver<TDbContext, string, TDbSessionInfo>>();
        }

        // Write methods

        public virtual async Task<DbSessionInfo> FindOrCreateAsync(
            TDbContext dbContext, string sessionId, CancellationToken cancellationToken = default)
        {
            var dbSessionInfo = await FindAsync(dbContext, sessionId, cancellationToken).ConfigureAwait(false);
            if (dbSessionInfo?.IsSignOutForced == true)
                throw Errors.ForcedSignOut();
            if (dbSessionInfo == null) {
                var sessionInfo = new SessionInfo(sessionId, Clock.Now);
                dbSessionInfo = dbContext.Add(
                    new TDbSessionInfo() {
                        Id = sessionInfo.Id,
                        CreatedAt = sessionInfo.CreatedAt,
                    }).Entity;
                dbSessionInfo.FromModel(sessionInfo);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            return dbSessionInfo;
        }

        public async Task<DbSessionInfo> CreateOrUpdateAsync(
            TDbContext dbContext, SessionInfo sessionInfo, CancellationToken cancellationToken = default)
        {
            var dbSessionInfo = await FindAsync(dbContext, sessionInfo.Id, cancellationToken).ConfigureAwait(false);
            dbSessionInfo ??= dbContext.Add(
                new TDbSessionInfo() {
                    Id = sessionInfo.Id,
                    CreatedAt = sessionInfo.CreatedAt,
                }).Entity;
            dbSessionInfo.FromModel(sessionInfo);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return dbSessionInfo;
        }

        public virtual async Task<int> TrimAsync(DateTime minLastSeenAt, int maxCount, CancellationToken cancellationToken = default)
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

        public async Task<DbSessionInfo?> FindAsync(string sessionId, CancellationToken cancellationToken = default)
            => await EntityResolver.TryGetAsync(sessionId, cancellationToken).ConfigureAwait(false);

        public virtual async Task<DbSessionInfo?> FindAsync(
            TDbContext dbContext, string sessionId, CancellationToken cancellationToken)
            => await dbContext.Set<TDbSessionInfo>()
                .FindAsync(ComposeKey(sessionId), cancellationToken)
                .ConfigureAwait(false);

        public virtual async Task<DbSessionInfo[]> ListByUserAsync(
            TDbContext dbContext, long userId, CancellationToken cancellationToken = default)
        {
            var qSessions =
                from s in  dbContext.Set<TDbSessionInfo>().AsQueryable()
                where s.UserId == userId
                orderby s.LastSeenAt descending
                select s;
            var sessions = (DbSessionInfo[]) await qSessions.ToArrayAsync(cancellationToken).ConfigureAwait(false);
            return sessions;
        }
    }
}
