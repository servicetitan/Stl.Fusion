using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Async;
using Stl.Fusion.Authentication;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Serialization;

namespace Stl.Fusion.EntityFramework.Authentication
{
    public interface IDbSessionInfoBackend<in TDbContext>
        where TDbContext : DbContext
    {
        ValueTask<SessionInfo> FromDbEntityAsync(
            TDbContext dbContext, DbSessionInfo dbSessionInfo, CancellationToken cancellationToken = default);

        // Write methods
        SessionInfo CreateGuestSessionInfo(string sessionId);
        Task<DbSessionInfo> CreateOrUpdateAsync(
            TDbContext dbContext, string sessionId,
            long? userId, bool? isSignOutForced,
            CancellationToken cancellationToken = default);
        Task<DbSessionInfo> CreateOrUpdateAsync(
            TDbContext dbContext, SessionInfo sessionInfo,
            long? userId, bool? isSignOutForced,
            CancellationToken cancellationToken = default);
        Task<int> TrimAsync(
            DateTime minLastSeenAt, int maxCount, CancellationToken cancellationToken = default);

        // Read methods
        Task<DbSessionInfo?> FindAsync(
            TDbContext dbContext, string sessionId, CancellationToken cancellationToken = default);
        Task<DbSessionInfo[]> ListByUserAsync(
            TDbContext dbContext, long userId, CancellationToken cancellationToken = default);
    }

    public class DbSessionInfoBackend<TDbContext, TDbSessionInfo> : DbServiceBase<TDbContext>, IDbSessionInfoBackend<TDbContext>
        where TDbContext : DbContext
        where TDbSessionInfo : DbSessionInfo, new()
    {
        protected DbAuthService<TDbContext>.Options Options { get; }

        public DbSessionInfoBackend(DbAuthService<TDbContext>.Options options, ServiceProvider services)
            : base(services)
            => Options = options;

        public virtual ValueTask<SessionInfo> FromDbEntityAsync(TDbContext dbContext, DbSessionInfo dbSessionInfo, CancellationToken cancellationToken)
        {
            if (dbSessionInfo.IsSignOutForced)
                throw Errors.CannotUseForcedSignOutSession();

            var sessionInfo = new SessionInfo() {
                Id = dbSessionInfo.Id,
                CreatedAt = dbSessionInfo.CreatedAt,
                LastSeenAt = dbSessionInfo.LastSeenAt,
                IPAddress = dbSessionInfo.IPAddress,
                UserAgent = dbSessionInfo.UserAgent,
                ExtraProperties = new ReadOnlyDictionary<string, object>(
                    FromJson<Dictionary<string, object>>(dbSessionInfo.ExtraPropertiesJson) ?? new()),
            };
            return ValueTaskEx.FromResult(sessionInfo)!;
        }

        public virtual SessionInfo CreateGuestSessionInfo(string sessionId)
        {
            var now = Clock.Now;
            return new SessionInfo(sessionId) {
                CreatedAt = now,
                LastSeenAt = now,
            };
        }

        // Write methods

        public virtual async Task<DbSessionInfo> CreateOrUpdateAsync(
            TDbContext dbContext, string sessionId,
            long? userId, bool? isSignOutForced,
            CancellationToken cancellationToken = default)
        {
            var dbSessionInfo = await FindAsync(dbContext, sessionId, cancellationToken).ConfigureAwait(false);
            if (dbSessionInfo != null) {
                if (dbSessionInfo.IsSignOutForced)
                    throw Errors.CannotUseForcedSignOutSession();
                if (userId.HasValue)
                    dbSessionInfo.UserId = userId.Value;
                if (isSignOutForced.HasValue)
                    dbSessionInfo.IsSignOutForced = isSignOutForced.Value;
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return dbSessionInfo;
            }

            var now = Clock.Now.ToDateTime();
            dbSessionInfo = dbContext.Add(new TDbSessionInfo()).Entity;
            dbSessionInfo.Id = sessionId;
            dbSessionInfo.CreatedAt = now;
            dbSessionInfo.LastSeenAt = now;
            dbSessionInfo.UserId = userId;
            dbSessionInfo.IsSignOutForced = isSignOutForced ?? false;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return dbSessionInfo;
        }

        public virtual async Task<DbSessionInfo> CreateOrUpdateAsync(
            TDbContext dbContext, SessionInfo sessionInfo,
            long? userId, bool? isSignOutForced,
            CancellationToken cancellationToken = default)
        {
            var dbSessionInfo = await FindAsync(dbContext, sessionInfo.Id, cancellationToken).ConfigureAwait(false);
            if (dbSessionInfo == null) {
                var now = Clock.Now.ToDateTime();
                dbSessionInfo = dbContext.Add(new TDbSessionInfo()).Entity;
                dbSessionInfo.Id = sessionInfo.Id;
                dbSessionInfo.CreatedAt = now;
            }
            else if (dbSessionInfo.IsSignOutForced)
                throw Errors.CannotUseForcedSignOutSession();

            dbSessionInfo.LastSeenAt = sessionInfo.LastSeenAt;
            dbSessionInfo.IPAddress = sessionInfo.IPAddress;
            dbSessionInfo.UserAgent = sessionInfo.UserAgent;
            if (userId.HasValue)
                dbSessionInfo.UserId = userId.Value;
            if (isSignOutForced.HasValue)
                dbSessionInfo.IsSignOutForced = isSignOutForced.Value;
            dbSessionInfo.ExtraPropertiesJson = ToJson(sessionInfo.ExtraProperties!.ToDictionary(kv => kv.Key, kv => kv.Value));
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

        public virtual async Task<DbSessionInfo?> FindAsync(
            TDbContext dbContext, string sessionId, CancellationToken cancellationToken)
            => await dbContext.Set<TDbSessionInfo>().FindAsync(sessionId, cancellationToken).ConfigureAwait(false);

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

        // Protected methods

        protected virtual string ToJson<T>(T source)
            => JsonSerialized.New(source).SerializedValue;

        protected virtual T? FromJson<T>(string json)
            => JsonSerialized.New<T>(json).Value;
    }
}
