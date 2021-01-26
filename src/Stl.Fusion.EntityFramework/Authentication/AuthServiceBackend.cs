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
using Stl.Internal;
using Stl.Serialization;

namespace Stl.Fusion.EntityFramework.Authentication
{
    public interface IAuthServiceBackend<in TDbContext>
        where TDbContext : DbContext
    {
        string PrimaryAuthenticationType { get; }

        string ToJson<T>(T source);
        T? FromJson<T>(string json);

        User CreateAnonymousUser(string sessionId);
        SessionInfo CreateSession(string sessionId);

        ValueTask<User> FromDbEntityAsync(
            TDbContext dbContext, DbUser dbUser, CancellationToken cancellationToken = default);
        ValueTask<SessionInfo> FromDbEntityAsync(
            TDbContext dbContext, DbSession dbSession, CancellationToken cancellationToken = default);

        Task<DbSession?> TryGetDbSessionAsync(
            TDbContext dbContext, string sessionId, CancellationToken cancellationToken = default);
        Task<DbUser?> TryGetDbUserAsync(
            TDbContext dbContext, long userId, CancellationToken cancellationToken = default);
        Task<DbSession[]> GetUserDbSessionsAsync(
            TDbContext dbContext, long userId, CancellationToken cancellationToken = default);

        Task<(DbUser User, DbExternalUserRef ExternalUserRef)> GetOrCreateDbUserAsync(
            TDbContext dbContext, User user, CancellationToken cancellationToken = default);
        Task<DbSession> GetOrCreateDbSessionAsync(
            TDbContext dbContext, Session session, CancellationToken cancellationToken = default);
    }

    public class AuthServiceBackend<TDbContext, TDbSession, TDbUser, TDbExternalUser> : DbServiceBase<TDbContext>,
        IAuthServiceBackend<TDbContext>
        where TDbContext : DbContext
        where TDbSession : DbSession, new()
        where TDbUser : DbUser, new()
        where TDbExternalUser : DbExternalUserRef, new()
    {
        public class Options
        {
            public string PrimaryAuthenticationType { get; set; } = "";
        }

        public string PrimaryAuthenticationType { get; }

        public AuthServiceBackend(Options options, ServiceProvider services) : base(services)
            => PrimaryAuthenticationType = options.PrimaryAuthenticationType;

        public virtual string ToJson<T>(T source)
            => JsonSerialized.New(source).SerializedValue;

        public virtual T? FromJson<T>(string json)
            => JsonSerialized.New<T>(json).Value;

        public User CreateAnonymousUser(string sessionId)
            => new($"Anonymous|{sessionId}");

        public SessionInfo CreateSession(string sessionId)
        {
            var now = Clock.Now;
            return new SessionInfo(sessionId) {
                CreatedAt = now,
                LastSeenAt = now,
            };
        }

        public ValueTask<User> FromDbEntityAsync(TDbContext dbContext, DbUser dbUser, CancellationToken cancellationToken)
        {
            var user = new User(
                PrimaryAuthenticationType,
                dbUser.Id.ToString(), dbUser.Name,
                new ReadOnlyDictionary<string, string>(
                    FromJson<Dictionary<string, string>>(dbUser.ClaimsJson) ?? new()));
            return ValueTaskEx.FromResult(user);
        }

        public ValueTask<SessionInfo> FromDbEntityAsync(TDbContext dbContext, DbSession dbSession, CancellationToken cancellationToken)
        {
            var sessionInfo = new SessionInfo() {
                Id = dbSession.Id,
                CreatedAt = dbSession.CreatedAt,
                LastSeenAt = dbSession.LastSeenAt,
                IPAddress = dbSession.IPAddress,
                UserAgent = dbSession.UserAgent,
                ExtraProperties = new ReadOnlyDictionary<string, object>(
                    FromJson<Dictionary<string, object>>(dbSession.ExtraPropertiesJson) ?? new()),
            };
            return ValueTaskEx.FromResult(sessionInfo);
        }

        public async Task<DbSession?> TryGetDbSessionAsync(
            TDbContext dbContext, string sessionId, CancellationToken cancellationToken)
            => await dbContext.Set<TDbSession>().FindAsync(sessionId, cancellationToken).ConfigureAwait(false);

        public async Task<DbUser?> TryGetDbUserAsync(
            TDbContext dbContext, long userId, CancellationToken cancellationToken)
            => await dbContext.Set<TDbUser>().FindAsync(userId, cancellationToken).ConfigureAwait(false);

        public async Task<DbSession[]> GetUserDbSessionsAsync(
            TDbContext dbContext, long userId, CancellationToken cancellationToken = default)
        {
            var qSessions =
                from s in  dbContext.Set<TDbSession>().AsQueryable()
                where s.UserId == userId
                orderby s.LastSeenAt descending
                select s;
            var sessions = (DbSession[]) await qSessions.ToArrayAsync(cancellationToken).ConfigureAwait(false);
            return sessions;
        }

        public virtual async Task<(DbUser User, DbExternalUserRef ExternalUserRef)> GetOrCreateDbUserAsync(
            TDbContext dbContext, User user, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(user.AuthenticationType))
                throw new ArgumentOutOfRangeException(nameof(user), "Can't create unauthenticated user.");

            var qFindUsers = (
                from eu in dbContext.Set<TDbExternalUser>().AsQueryable()
                where eu.Id == user.Id
                from u in dbContext.Set<TDbUser>().AsQueryable().Where(u => u.Id == eu.UserId)
                select (u, eu)
                ).Take(2);
            var dbUsers = await qFindUsers.ToListAsync(cancellationToken).ConfigureAwait(false);

            switch (dbUsers.Count) {
            case 1:
                return dbUsers[0];
            case 0:
                var dbUser = new TDbUser() {
                    Name = user.Name,
                    ClaimsJson = ToJson(user.Claims.ToDictionary(kv => kv.Key, kv => kv.Value)),
                };
                await dbContext.AddAsync(dbUser, cancellationToken).ConfigureAwait(false);
                await dbContext.SaveChangesAsync(cancellationToken);

                var dbExtUser = new TDbExternalUser() {
                    Id = user.Id,
                    UserId = dbUser.Id,
                };
                await dbContext.AddAsync(dbExtUser, cancellationToken).ConfigureAwait(false);
                await dbContext.SaveChangesAsync(cancellationToken);
                return (dbUser, dbExtUser);
            default:
                throw Errors.InternalError(
                    $"Two {typeof(TDbUser).Name} entities relate to the same {typeof(TDbExternalUser).Name} entity.");
            }
        }

        public virtual async Task<DbSession> GetOrCreateDbSessionAsync(
            TDbContext dbContext, Session session, CancellationToken cancellationToken)
        {
            var dbSession = await dbContext.Set<TDbSession>().FindAsync(session.Id).ConfigureAwait(false);
            if (dbSession == null) {
                var now = Clock.Now.ToDateTime();
                dbSession = new TDbSession() {
                    Id = session.Id,
                    CreatedAt = now,
                    LastSeenAt = now,
                };
                await dbContext.AddAsync(dbSession, cancellationToken);
            }
            return dbSession;
        }
    }
}
