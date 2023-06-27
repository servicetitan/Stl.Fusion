using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework;
using Stl.Multitenancy;

namespace Stl.Fusion.Authentication.Services;

// ReSharper disable once TypeParameterCanBeVariant
public interface IDbUserRepo<in TDbContext, TDbUser, TDbUserId>
    where TDbContext : DbContext
    where TDbUser : DbUser<TDbUserId>, new()
    where TDbUserId : notnull
{
    Type UserEntityType { get; }

    // Write methods
    Task<TDbUser> Create(TDbContext dbContext, User user, CancellationToken cancellationToken = default);
    Task<(TDbUser DbUser, bool IsCreated)> GetOrCreateOnSignIn(
        TDbContext dbContext, User user, CancellationToken cancellationToken = default);
    Task Edit(
        TDbContext dbContext, TDbUser dbUser, Auth_EditUser command, CancellationToken cancellationToken = default);
    Task Remove(
        TDbContext dbContext, TDbUser dbUser, CancellationToken cancellationToken = default);

    // Read methods
    Task<TDbUser?> Get(Tenant tenant, TDbUserId userId, CancellationToken cancellationToken = default);
    Task<TDbUser?> Get(TDbContext dbContext, TDbUserId userId, bool forUpdate, CancellationToken cancellationToken = default);
    Task<TDbUser?> GetByUserIdentity(
        TDbContext dbContext, UserIdentity userIdentity, bool forUpdate, CancellationToken cancellationToken = default);
}

public class DbUserRepo<TDbContext, TDbUser, TDbUserId> : DbServiceBase<TDbContext>,
    IDbUserRepo<TDbContext, TDbUser, TDbUserId>
    where TDbContext : DbContext
    where TDbUser : DbUser<TDbUserId>, new()
    where TDbUserId : notnull
{
    protected DbAuthService<TDbContext>.Options Options { get; init; }
    protected IDbUserIdHandler<TDbUserId> DbUserIdHandler { get; init; }
    protected IDbEntityResolver<TDbUserId, TDbUser> UserResolver { get; init; }
    protected IDbEntityConverter<TDbUser, User> UserConverter { get; init; }

    public Type UserEntityType => typeof(TDbUser);

    public DbUserRepo(DbAuthService<TDbContext>.Options options, IServiceProvider services)
        : base(services)
    {
        Options = options;
        DbUserIdHandler = services.GetRequiredService<IDbUserIdHandler<TDbUserId>>();
        UserResolver = services.DbEntityResolver<TDbUserId, TDbUser>();
        UserConverter = services.DbEntityConverter<TDbUser, User>();
    }

    // Write methods

    public virtual async Task<TDbUser> Create(
        TDbContext dbContext, User user, CancellationToken cancellationToken = default)
    {
        // Creating "base" dbUser
        var id = DbUserIdHandler.Parse(user.Id, true);
        if (DbUserIdHandler.IsNone(id))
            id = DbUserIdHandler.New();
        var dbUser = new TDbUser() {
            Id = id,
            Version = VersionGenerator.NextVersion(),
            Name = user.Name,
            Claims = user.Claims,
        };
        dbContext.Add(dbUser);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        user = user with {
            Id = DbUserIdHandler.Format(dbUser.Id)
        };
        // Updating dbUser from the model to persist user.Identities
        UserConverter.UpdateEntity(user, dbUser);
        dbContext.Update(dbUser);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return dbUser;
    }

    public virtual async Task<(TDbUser DbUser, bool IsCreated)> GetOrCreateOnSignIn(
        TDbContext dbContext, User user, CancellationToken cancellationToken = default)
    {
        var dbUserId = DbUserIdHandler.Parse(user.Id, true);
        TDbUser? dbUser;
        if (!DbUserIdHandler.IsNone(dbUserId)) {
            dbUser = await Get(dbContext, dbUserId, false, cancellationToken).ConfigureAwait(false);
            if (dbUser != null)
                return (dbUser, false);
        }

        // No user found, let's create it
        dbUser = await Create(dbContext, user, cancellationToken).ConfigureAwait(false);
        return (dbUser, true);
    }

    public virtual async Task Edit(TDbContext dbContext, TDbUser dbUser, Auth_EditUser command,
        CancellationToken cancellationToken = default)
    {
        if (command.Name != null) {
            dbUser.Name = command.Name;
            dbUser.Version = VersionGenerator.NextVersion(dbUser.Version);
        }
        dbContext.Update(dbUser);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task Remove(
        TDbContext dbContext, TDbUser dbUser, CancellationToken cancellationToken = default)
    {
        await dbContext.Entry(dbUser).Collection(nameof(DbUser<object>.Identities))
            .LoadAsync(cancellationToken).ConfigureAwait(false);
        if (dbUser.Identities.Count > 0)
            dbContext.RemoveRange(dbUser.Identities);
        dbContext.Remove(dbUser);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    // Read methods

    public async Task<TDbUser?> Get(Tenant tenant, TDbUserId userId, CancellationToken cancellationToken = default)
        => await UserResolver.Get(tenant, userId, cancellationToken).ConfigureAwait(false);

    public virtual async Task<TDbUser?> Get(
        TDbContext dbContext, TDbUserId userId, bool forUpdate, CancellationToken cancellationToken = default)
    {
        var dbUsers = forUpdate
            ? dbContext.Set<TDbUser>().ForUpdate()
            : dbContext.Set<TDbUser>();
        var dbUser = await dbUsers
            .FirstOrDefaultAsync(u => Equals(u.Id, userId), cancellationToken)
            .ConfigureAwait(false);
        if (dbUser != null)
            await dbContext.Entry(dbUser).Collection(nameof(DbUser<object>.Identities))
                .LoadAsync(cancellationToken).ConfigureAwait(false);
        return dbUser;
    }

    public virtual async Task<TDbUser?> GetByUserIdentity(
        TDbContext dbContext, UserIdentity userIdentity, bool forUpdate, CancellationToken cancellationToken = default)
    {
        if (!userIdentity.IsValid)
            return null;
        var dbUserIdentities = forUpdate
            ? dbContext.Set<DbUserIdentity<TDbUserId>>().ForUpdate()
            : dbContext.Set<DbUserIdentity<TDbUserId>>();
        var id = userIdentity.Id.Value;
        var dbUserIdentity = await dbUserIdentities
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (dbUserIdentity == null)
            return null;
        var user = await Get(dbContext, dbUserIdentity.DbUserId, forUpdate, cancellationToken).ConfigureAwait(false);
        return user;
    }
}
