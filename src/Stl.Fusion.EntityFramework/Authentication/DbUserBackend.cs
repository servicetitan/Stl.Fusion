using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Stl.Async;
using Stl.Fusion.Authentication;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Serialization;

namespace Stl.Fusion.EntityFramework.Authentication
{
    public interface IDbUserBackend<in TDbContext>
        where TDbContext : DbContext
    {
        ValueTask<User> FromDbEntityAsync(
            TDbContext dbContext, DbUser dbUser, CancellationToken cancellationToken = default);

        // Write methods
        User CreateGuestUser(string sessionId);
        Task<DbUser> CreateOrUpdateAsync(
            TDbContext dbContext, User user, CancellationToken cancellationToken = default);
        Task RemoveAsync(
            TDbContext dbContext, DbUser dbUser, CancellationToken cancellationToken = default);

        // Read methods
        Task<DbUser?> FindAsync(
            TDbContext dbContext, long userId, CancellationToken cancellationToken = default);
        Task<DbUser?> FindByExternalUserIdAsync(
            TDbContext dbContext, string? externalUserId, CancellationToken cancellationToken = default);
    }

    public class DbUserBackend<TDbContext, TDbUser, TDbExternalUser> : DbServiceBase<TDbContext>,
        IDbUserBackend<TDbContext>
        where TDbContext : DbContext
        where TDbUser : DbUser, new()
        where TDbExternalUser : DbExternalUser, new()
    {
        protected DbAuthService<TDbContext>.Options Options { get; }

        public DbUserBackend(DbAuthService<TDbContext>.Options options, IServiceProvider services)
            : base(services)
            => Options = options;

        public virtual User CreateGuestUser(string sessionId)
            => new(sessionId);

        public virtual ValueTask<User> FromDbEntityAsync(
            TDbContext dbContext, DbUser dbUser, CancellationToken cancellationToken = default)
        {
            var user = new User(
                dbUser.AuthenticationType,
                dbUser.Id.ToString(),
                dbUser.Name,
                (FromJson<Dictionary<string, string>>(dbUser.ClaimsJson) ?? new())
                    .ToImmutableDictionary());
            return ValueTaskEx.FromResult(user);
        }

        // Write methods

        public virtual async Task<DbUser> CreateOrUpdateAsync(
            TDbContext dbContext, User user, CancellationToken cancellationToken = default)
        {
            if (!user.IsAuthenticated)
                throw Errors.AuthenticatedUserRequired();

            // Trying to find user by its Id
            var hasId = !string.IsNullOrEmpty(user.Id);
            var dbUser = hasId
                ? await FindAsync(dbContext, long.Parse(user.Id), cancellationToken).ConfigureAwait(false)
                : null;
            if (dbUser == null && hasId)
                throw Errors.UserNotFound();

            // Id wasn't provided, so let's try to find it by its external Id or just create a new one
            var externalId = user.TryGetExternalId();
            var userByExternalId = await FindByExternalUserIdAsync(dbContext, externalId, cancellationToken)
                .ConfigureAwait(false);
            dbUser ??= userByExternalId ?? dbContext.Add(new TDbUser()).Entity;

            // Updating user properties
            dbUser.AuthenticationType = user.AuthenticationType;
            dbUser.Name = user.Name;
            dbUser.ClaimsJson = ToJson(user.Claims.ToDictionary(kv => kv.Key, kv => kv.Value));
            await dbContext.SaveChangesAsync(cancellationToken);

            // Adding TDbExternalUser record, if needed
            if (!string.IsNullOrEmpty(externalId) && userByExternalId == null) {
                dbContext.Add(new TDbExternalUser() {
                    ExternalId = externalId,
                    UserId = dbUser.Id,
                });
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            return dbUser;
        }

        public virtual async Task RemoveAsync(
            TDbContext dbContext, DbUser dbUser, CancellationToken cancellationToken = default)
        {
            var externalUsers = await dbContext.Set<TDbExternalUser>().AsQueryable()
                .Where(eu => eu.UserId == dbUser.Id)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            if (externalUsers.Count > 0) {
                dbContext.RemoveRange(externalUsers);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            dbContext.Remove(dbUser);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // Read methods

        public virtual async Task<DbUser?> FindAsync(
            TDbContext dbContext, long userId, CancellationToken cancellationToken)
            => await dbContext.Set<TDbUser>()
                .FindAsync(Key(userId), cancellationToken)
                .ConfigureAwait(false);

        public virtual async Task<DbUser?> FindByExternalUserIdAsync(
            TDbContext dbContext, string? externalUserId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(externalUserId))
                return null;
            var externalUser = await dbContext.Set<TDbExternalUser>()
                .FindAsync(Key(externalUserId), cancellationToken)
                .ConfigureAwait(false);
            if (externalUser == null)
                return null;
            var user = await FindAsync(dbContext, externalUser.UserId, cancellationToken).ConfigureAwait(false);
            return user;
        }

        // Protected methods

        protected virtual string ToJson<T>(T source)
            => JsonSerialized.New(source).SerializedValue;

        protected virtual T? FromJson<T>(string json)
            => JsonSerialized.New<T>(json).Value;

        protected object[] Key(params object[] components)
            => components;
    }
}
