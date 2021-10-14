using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;
using Stl.Fusion.EntityFramework.Internal;

namespace Stl.Fusion.EntityFramework.Authentication;

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
        TDbContext dbContext, TDbUser dbUser, EditUserCommand command, CancellationToken cancellationToken = default);
    Task Remove(
        TDbContext dbContext, TDbUser dbUser, CancellationToken cancellationToken = default);

    // Read methods
    Task<TDbUser?> TryGet(TDbUserId userId, CancellationToken cancellationToken = default);
    Task<TDbUser?> TryGet(
        TDbContext dbContext, TDbUserId userId, CancellationToken cancellationToken = default);
    Task<TDbUser?> TryGetByUserIdentity(
        TDbContext dbContext, UserIdentity userIdentity, CancellationToken cancellationToken = default);
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
        var dbUser = new TDbUser() {
            Id = DbUserIdHandler.New(),
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
        TDbContext dbContext, User user, CancellationToken cancellationToken)
    {
        TDbUser dbUser;
        if (!string.IsNullOrEmpty(user.Id)) {
            dbUser = await TryGet(dbContext, DbUserIdHandler.Parse(user.Id), cancellationToken).ConfigureAwait(false)
                ?? throw Errors.EntityNotFound<TDbUser>();
            return (dbUser, false);
        }

        // No user found, let's create it
        dbUser = await Create(dbContext, user, cancellationToken).ConfigureAwait(false);
        return (dbUser, true);
    }

    public virtual async Task Edit(TDbContext dbContext, TDbUser dbUser, EditUserCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Name != null) {
            dbUser.Name = command.Name;
            dbUser.Version = VersionGenerator.NextVersion(dbUser.Version);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
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

    public async Task<TDbUser?> TryGet(TDbUserId userId, CancellationToken cancellationToken = default)
        => await UserResolver.TryGet(userId, cancellationToken).ConfigureAwait(false);

    public virtual async Task<TDbUser?> TryGet(
        TDbContext dbContext, TDbUserId userId, CancellationToken cancellationToken)
    {
        var dbUser = await dbContext.Set<TDbUser>()
            .FindAsync(ComposeKey(userId), cancellationToken)
            .ConfigureAwait(false);
        if (dbUser != null)
            await dbContext.Entry(dbUser).Collection(nameof(DbUser<object>.Identities))
                .LoadAsync(cancellationToken).ConfigureAwait(false);
        return dbUser;
    }

    public virtual async Task<TDbUser?> TryGetByUserIdentity(
        TDbContext dbContext, UserIdentity userIdentity, CancellationToken cancellationToken = default)
    {
        if (!userIdentity.IsValid)
            return null;
        var dbUserIdentities = await dbContext.Set<DbUserIdentity<TDbUserId>>()
            .FindAsync(ComposeKey(userIdentity.Id.Value), cancellationToken)
            .ConfigureAwait(false);
        if (dbUserIdentities == null)
            return null;
        var user = await TryGet(dbContext, dbUserIdentities.DbUserId, cancellationToken).ConfigureAwait(false);
        return user;
    }
}
