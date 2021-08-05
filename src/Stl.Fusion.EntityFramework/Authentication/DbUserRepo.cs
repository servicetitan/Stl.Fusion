using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;
using Stl.Fusion.EntityFramework.Internal;

namespace Stl.Fusion.EntityFramework.Authentication
{
    public interface IDbUserRepo<in TDbContext>
        where TDbContext : DbContext
    {
        Type UserEntityType { get; }

        // Write methods
        Task<DbUser> Create(TDbContext dbContext, User user, CancellationToken cancellationToken = default);
        Task<(DbUser DbUser, bool IsCreated)> GetOrCreateOnSignIn(
            TDbContext dbContext, User user, CancellationToken cancellationToken = default);
        Task Edit(
            TDbContext dbContext, DbUser dbUser, EditUserCommand command, CancellationToken cancellationToken = default);
        Task Remove(
            TDbContext dbContext, DbUser dbUser, CancellationToken cancellationToken = default);

        // Read methods
        Task<DbUser?> TryGet(long userId, CancellationToken cancellationToken = default);
        Task<DbUser?> TryGet(
            TDbContext dbContext, long userId, CancellationToken cancellationToken = default);
        Task<DbUser?> TryGetByUserIdentity(
            TDbContext dbContext, UserIdentity userIdentity, CancellationToken cancellationToken = default);
    }

    public class DbUserRepo<TDbContext, TDbUser> : DbServiceBase<TDbContext>,
        IDbUserRepo<TDbContext>
        where TDbContext : DbContext
        where TDbUser : DbUser, new()
    {
        protected DbAuthService<TDbContext>.Options Options { get; }
        protected DbEntityResolver<TDbContext, long, TDbUser> EntityResolver { get; }

        public Type UserEntityType => typeof(TDbUser);

        public DbUserRepo(DbAuthService<TDbContext>.Options options, IServiceProvider services)
            : base(services)
        {
            Options = options;
            EntityResolver = services.GetRequiredService<DbEntityResolver<TDbContext, long, TDbUser>>();
        }

        // Write methods

        public virtual async Task<DbUser> Create(
            TDbContext dbContext, User user, CancellationToken cancellationToken = default)
        {
            var dbUser = new TDbUser() {
                Name = user.Name,
                Claims = user.Claims,
            };
            dbContext.Add(dbUser);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            user = user with { Id = dbUser.Id.ToString() };
            dbUser.UpdateFrom(user);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return dbUser;
        }

        public virtual async Task<(DbUser DbUser, bool IsCreated)> GetOrCreateOnSignIn(
            TDbContext dbContext, User user, CancellationToken cancellationToken)
        {
            DbUser dbUser;
            if (!string.IsNullOrEmpty(user.Id)) {
                dbUser = await TryGet(dbContext, long.Parse(user.Id), cancellationToken).ConfigureAwait(false)
                    ?? throw Errors.EntityNotFound<TDbUser>();
                return (dbUser, false);
            }

            // No user found, let's create it
            dbUser = await Create(dbContext, user, cancellationToken).ConfigureAwait(false);
            return (dbUser, true);
        }

        public virtual async Task Edit(TDbContext dbContext, DbUser dbUser, EditUserCommand command,
            CancellationToken cancellationToken = default)
        {
            if (command.Name != null)
                dbUser.Name = command.Name;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task Remove(
            TDbContext dbContext, DbUser dbUser, CancellationToken cancellationToken = default)
        {
            await dbContext.Entry(dbUser).Collection(nameof(DbUser.Identities))
                .LoadAsync(cancellationToken).ConfigureAwait(false);
            if (dbUser.Identities.Count > 0)
                dbContext.RemoveRange(dbUser.Identities);
            dbContext.Remove(dbUser);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // Read methods

        public async Task<DbUser?> TryGet(long userId, CancellationToken cancellationToken = default)
            => await EntityResolver.TryGet(userId, cancellationToken).ConfigureAwait(false);

        public virtual async Task<DbUser?> TryGet(
            TDbContext dbContext, long userId, CancellationToken cancellationToken)
        {
            var dbUser = await dbContext.Set<TDbUser>()
                .FindAsync(ComposeKey(userId), cancellationToken)
                .ConfigureAwait(false);
            if (dbUser != null)
                await dbContext.Entry(dbUser).Collection(nameof(DbUser.Identities))
                    .LoadAsync(cancellationToken).ConfigureAwait(false);
            return dbUser;
        }

        public virtual async Task<DbUser?> TryGetByUserIdentity(
            TDbContext dbContext, UserIdentity userIdentity, CancellationToken cancellationToken = default)
        {
            if (!userIdentity.IsValid)
                return null;
            var dbUserIdentities = await dbContext.Set<DbUserIdentity>()
                .FindAsync(ComposeKey(userIdentity.Id.Value), cancellationToken)
                .ConfigureAwait(false);
            if (dbUserIdentities == null)
                return null;
            var user = await TryGet(dbContext, dbUserIdentities.DbUserId, cancellationToken).ConfigureAwait(false);
            return user;
        }
    }
}
