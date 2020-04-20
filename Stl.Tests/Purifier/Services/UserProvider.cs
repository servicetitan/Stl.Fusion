using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Purifier;
using Stl.Purifier.Autofac;
using Stl.Tests.Purifier.Model;

namespace Stl.Tests.Purifier.Services
{
    public interface IUserProvider
    {
        Task CreateAsync(User user, bool orUpdate = false, CancellationToken cancellationToken = default);
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(User user, CancellationToken cancellationToken = default);
        ValueTask<User?> TryGetAsync(long userId, CancellationToken cancellationToken = default);
        ValueTask<long> CountAsync(CancellationToken cancellationToken = default);
    }

    public class UserProvider : IUserProvider 
    {
        protected ILogger Log { get; }
        protected ITestDbContextPool DbContextPool { get; }

        public UserProvider(
            ITestDbContextPool dbContextPool,
            ILogger<UserProvider>? log = null)
        {
            Log = log as ILogger ?? NullLogger.Instance;
            DbContextPool = dbContextPool;
        }

        public virtual async Task CreateAsync(User user, bool orUpdate = false, CancellationToken cancellationToken = default)
        {
            using var lease = DbContextPool.Rent();
            var dbContext = lease.Item;
            var existingUser = (User?) null;

            var supportTransactions = !dbContext.Database.IsInMemory();
            await using var tx = supportTransactions 
                ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
                : (IDbContextTransaction?) null;

            var userId = user.Id;
            if (orUpdate) {
                existingUser = await dbContext.Users.FindAsync(new [] {(object) userId}, cancellationToken);
                if (existingUser != null)
                    dbContext.Users.Update(user);
            }
            if (existingUser == null)
                dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await (tx?.CommitAsync(cancellationToken) ?? Task.CompletedTask);
            OnChanged(user, existingUser == null);
        }

        public virtual async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            using var lease = DbContextPool.Rent();
            var dbContext = lease.Item;
            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            OnChanged(user, false);
        }

        [Computed(false)] // Needed b/c the signature fits for interception!
        public virtual async Task<bool> DeleteAsync(User user, CancellationToken cancellationToken = default)
        {
            Computed.GetCurrent().Should().BeNull();
            using var lease = DbContextPool.Rent();
            var dbContext = lease.Item;
            dbContext.Users.Remove(user);
            try {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                OnChanged(user);
                return true;
            }
            catch (DbUpdateConcurrencyException) {
                return false;
            }
        }

        public virtual async ValueTask<User?> TryGetAsync(long userId, CancellationToken cancellationToken = default)
        {
            using var lease = DbContextPool.Rent();
            var dbContext = lease.Item;
            var user = await dbContext.Users
                .FindAsync(new[] {(object) userId}, cancellationToken)
                .ConfigureAwait(false);
            user.Freeze();
            return user;
        }

        public virtual async ValueTask<long> CountAsync(CancellationToken cancellationToken = default) 
        {
            using var lease = DbContextPool.Rent();
            var dbContext = lease.Item;
            var count = await dbContext.Users.LongCountAsync(cancellationToken).ConfigureAwait(false);
            Log.LogDebug($"Users.Count query: {count}");
            return count;
        }

        // Change handling

        protected virtual async void OnChanged(User user, bool countChanged = true)
        {
            if (GetType() == typeof(UserProvider))
                // No caching interceptors, so nothing to do
                return;

            using var _ = ComputeContext.New(ComputeOptions.Invalidate);
            var u = await TryGetAsync(user.Id).ConfigureAwait(false);
            if (u != default)
                Log.LogDebug($"Invalidated: {user}");
            if (countChanged) {
                var c = await CountAsync().ConfigureAwait(false);
                if (c != default)
                    Log.LogDebug($"Invalidated: Users.Count");
            }
        }
    }
}
