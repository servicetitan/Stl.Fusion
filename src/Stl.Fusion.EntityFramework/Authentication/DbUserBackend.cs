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
            TDbContext dbContext, string externalUserId, CancellationToken cancellationToken = default);
    }

    public class DbUserBackend<TDbContext, TDbUser, TDbExternalUser> : DbServiceBase<TDbContext>,
        IDbUserBackend<TDbContext>
        where TDbContext : DbContext
        where TDbUser : DbUser, new()
        where TDbExternalUser : DbExternalUser, new()
    {
        protected DbAuthService<TDbContext>.Options Options { get; }

        public DbUserBackend(DbAuthService<TDbContext>.Options options, ServiceProvider services)
            : base(services)
            => Options = options;

        public virtual User CreateGuestUser(string sessionId)
            => new($"Anonymous|{sessionId}");

        public virtual ValueTask<User> FromDbEntityAsync(
            TDbContext dbContext, DbUser dbUser, CancellationToken cancellationToken = default)
        {
            var user = new User(
                Options.PrimaryAuthenticationType,
                dbUser.Id.ToString(),
                dbUser.Name,
                new ReadOnlyDictionary<string, string>(
                    FromJson<Dictionary<string, string>>(dbUser.ClaimsJson) ?? new()));
            return ValueTaskEx.FromResult(user);
        }

        // Write methods

        public virtual async Task<DbUser> CreateOrUpdateAsync(
            TDbContext dbContext, User user, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(user.AuthenticationType))
                throw Errors.CannotCreateUnauthenticatedUser(nameof(user));
            var externalUserId = GetExternalUserId(user);

            var dbUser = long.TryParse(user.Id, out var userId)
                ? await FindAsync(dbContext, userId, cancellationToken).ConfigureAwait(false)
                : null;
            dbUser ??= await FindByExternalUserIdAsync(dbContext, externalUserId, cancellationToken).ConfigureAwait(false);
            dbUser ??= dbContext.Add(new TDbUser()).Entity;

            dbUser.Name = user.Name;
            dbUser.ClaimsJson = ToJson(user.Claims.ToDictionary(kv => kv.Key, kv => kv.Value));
            await dbContext.SaveChangesAsync(cancellationToken);

            if (null == await FindByExternalUserIdAsync(dbContext, externalUserId, cancellationToken).ConfigureAwait(false)) {
                var dbExtUser = new TDbExternalUser() {
                    ExternalId = externalUserId,
                    UserId = dbUser.Id,
                };
                dbContext.Add(dbExtUser);
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
            => await dbContext.Set<TDbUser>().FindAsync(userId, cancellationToken).ConfigureAwait(false);

        public virtual async Task<DbUser?> FindByExternalUserIdAsync(
            TDbContext dbContext, string externalUserId, CancellationToken cancellationToken = default)
        {
            var externalUser = await dbContext.Set<TDbExternalUser>().FindAsync(externalUserId, cancellationToken).ConfigureAwait(false);
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

        protected virtual string GetExternalUserId(User user)
        {
            var externalIdClaim = $"Id:{user.AuthenticationType}";
            var externalId = user.Claims[externalIdClaim];
            return externalId;
        }
    }
}
