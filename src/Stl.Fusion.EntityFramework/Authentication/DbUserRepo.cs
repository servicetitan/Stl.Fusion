using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        Task<(DbUser DbUser, bool IsCreated)> FindOrCreateOnSignInAsync(
            TDbContext dbContext, User user, CancellationToken cancellationToken = default);
        Task EditAsync(
            TDbContext dbContext, DbUser dbUser, EditUserCommand command, CancellationToken cancellationToken = default);
        Task RemoveAsync(
            TDbContext dbContext, DbUser dbUser, CancellationToken cancellationToken = default);

        // Read methods
        Task<DbUser?> FindAsync(long userId, CancellationToken cancellationToken = default);
        Task<DbUser?> FindAsync(
            TDbContext dbContext, long userId, CancellationToken cancellationToken = default);
        Task<DbUser?> FindByIdentityAsync(
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

        public virtual async Task<(DbUser DbUser, bool IsCreated)> FindOrCreateOnSignInAsync(
            TDbContext dbContext, User user, CancellationToken cancellationToken)
        {
            DbUser dbUser;
            if (!string.IsNullOrEmpty(user.Id)) {
                dbUser = await FindAsync(dbContext, long.Parse(user.Id), cancellationToken).ConfigureAwait(false)
                    ?? throw Errors.EntityNotFound<TDbUser>();
                return (dbUser, false);
            }

            // No user found, let's create it
            dbUser = new TDbUser() {
                Name = user.Name,
                Claims = user.Claims,
            };
            dbContext.Add(dbUser);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            user = user with { Id = dbUser.Id.ToString() };
            dbUser.UpdateFrom(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            return (dbUser, true);
        }

        public virtual async Task EditAsync(TDbContext dbContext, DbUser dbUser, EditUserCommand command,
            CancellationToken cancellationToken = default)
        {
            var (_, name) = command;
            if (name != null)
                dbUser.Name = name;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task RemoveAsync(
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

        public async Task<DbUser?> FindAsync(long userId, CancellationToken cancellationToken = default)
            => await EntityResolver.TryGetAsync(userId, cancellationToken).ConfigureAwait(false);

        public virtual async Task<DbUser?> FindAsync(
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

        public virtual async Task<DbUser?> FindByIdentityAsync(
            TDbContext dbContext, UserIdentity userIdentity, CancellationToken cancellationToken = default)
        {
            if (!userIdentity.IsValid)
                return null;
            var dbUserIdentities = await dbContext.Set<DbUserIdentity>()
                .FindAsync(ComposeKey(userIdentity.Id.Value), cancellationToken)
                .ConfigureAwait(false);
            if (dbUserIdentities == null)
                return null;
            var user = await FindAsync(dbContext, dbUserIdentities.DbUserId, cancellationToken).ConfigureAwait(false);
            return user;
        }
    }
}
