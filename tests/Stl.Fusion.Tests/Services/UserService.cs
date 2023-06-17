using MemoryPack;
using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.Tests.Model;
using Stl.Reflection;

namespace Stl.Fusion.Tests.Services;

public interface IUserService : IComputeService
{
    [CommandHandler]
    Task Create(UserService_Add command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task Update(UserService_Update command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task<bool> Delete(UserService_Delete command, CancellationToken cancellationToken = default);

    [ComputeMethod(MinCacheDuration = 60)]
    Task<User?> Get(long userId, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 60)]
    Task<long> Count(CancellationToken cancellationToken = default);

    Task UpdateDirectly(UserService_Update command, CancellationToken cancellationToken = default);
    Task Invalidate();
}

[DataContract, MemoryPackable]
// ReSharper disable once InconsistentNaming
public partial record UserService_Add(
    [property: DataMember] User User,
    [property: DataMember] bool OrUpdate = false
) : ICommand<Unit>;

[DataContract, MemoryPackable]
// ReSharper disable once InconsistentNaming
public partial record UserService_Update(
    [property: DataMember] User User
) : ICommand<Unit>;

[DataContract, MemoryPackable]
// ReSharper disable once InconsistentNaming
public partial record UserService_Delete(
    [property: DataMember] User User
) : ICommand<bool>;

public class UserService : DbServiceBase<TestDbContext>, IUserService
{
    public bool IsProxy { get; }

    public UserService(IServiceProvider services) : base(services)
    {
        var type = GetType();
        IsProxy = type != type.NonProxyType();
    }

    public virtual async Task Create(UserService_Add command, CancellationToken cancellationToken = default)
    {
        var (user, orUpdate) = command;
        var existingUser = (User?) null;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            _ = Get(user.Id, default).AssertCompleted();
            existingUser = context.Operation().Items.Get<User>();
            if (existingUser == null)
                _ = Count(default).AssertCompleted();
            return;
        }

        var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
        await using var _1 = dbContext.ConfigureAwait(false);
        dbContext.DisableChangeTracking();

        var userId = user.Id;
        if (orUpdate) {
            existingUser = await dbContext.Users.FindAsync(DbKey.Compose(userId), cancellationToken);
            context.Operation().Items.Set(existingUser);
            if (existingUser != null!)
                dbContext.Users.Update(user);
        }
        if (existingUser == null)
            dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task Update(UserService_Update command, CancellationToken cancellationToken = default)
    {
        var user = command.User;
        if (Computed.IsInvalidating()) {
            _ = Get(user.Id, default).AssertCompleted();
            return;
        }

        var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
        await using var _1 = dbContext.ConfigureAwait(false);

        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateDirectly(UserService_Update command, CancellationToken cancellationToken = default)
    {
        var user = command.User;
        await using (var dbContext = CreateDbContext(true)) {
            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        if (Computed.IsInvalidating())
            _ = Get(user.Id, default).AssertCompleted();
    }

    public virtual async Task<bool> Delete(UserService_Delete command, CancellationToken cancellationToken = default)
    {
        var user = command.User;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            var success = context.Operation().Items.GetOrDefault<bool>();
            if (success) {
                _ = Get(user.Id, default).AssertCompleted();
                _ = Count(default).AssertCompleted();
            }
            return false;
        }

        var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
        await using var _1 = dbContext.ConfigureAwait(false);

        dbContext.Users.Remove(user);
        try {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            context.Operation().Items.Set(true);
            return true;
        }
        catch (DbUpdateConcurrencyException) {
            return false;
        }
    }

    public virtual async Task<User?> Get(long userId, CancellationToken cancellationToken = default)
    {
        // Debug.WriteLine($"Get {userId}");
        await Everything().ConfigureAwait(false);

        var dbContext = CreateDbContext();
        await using var _ = dbContext.ConfigureAwait(false);

        var user = await dbContext.Users
            .FindAsync(new[] {(object) userId}, cancellationToken)
            .ConfigureAwait(false);
        return user;
    }

    public virtual async Task<long> Count(CancellationToken cancellationToken = default)
    {
        await Everything().ConfigureAwait(false);

        var dbContext = CreateDbContext();
        await using var _ = dbContext.ConfigureAwait(false);

        var count = await dbContext.Users.AsQueryable()
            .LongCountAsync(cancellationToken)
            .ConfigureAwait(false);
        // _log.LogDebug($"Users.Count query: {count}");
        return count;
    }

    public virtual Task Invalidate()
    {
        if (!IsProxy)
            return Task.CompletedTask;

        using (Computed.Invalidate())
            _ = Everything().AssertCompleted();

        return Task.CompletedTask;
    }

    // Protected & private methods

    [ComputeMethod]
    protected virtual Task<Unit> Everything() => TaskExt.UnitTask;

    private new Task<TestDbContext> CreateCommandDbContext(CancellationToken cancellationToken = default)
    {
        if (IsProxy)
            return base.CreateCommandDbContext(cancellationToken);
        return Task.FromResult(CreateDbContext().ReadWrite());
    }
}
