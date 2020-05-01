using System;
using System.Diagnostics;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.Fusion;
using Stl.Fusion.Autofac;
using Stl.Tests.Fusion.Model;

namespace Stl.Tests.Fusion.Services
{
    public interface IUserProvider
    {
        Task CreateAsync(User user, bool orUpdate = false, CancellationToken cancellationToken = default);
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(User user, CancellationToken cancellationToken = default);
        Task<User?> TryGetAsync(long userId, CancellationToken cancellationToken = default);
        Task<long> CountAsync(CancellationToken cancellationToken = default);
        void Invalidate();
    }

    public class UserProvider : IUserProvider 
    {
        protected ILogger Log { get; }
        protected ITestDbContextPool DbContextPool { get; }
        protected bool IsCaching { get; }

        public UserProvider(
            ITestDbContextPool dbContextPool,
            ILogger<UserProvider>? log = null)
        {
            Log = log as ILogger ?? NullLogger.Instance;
            DbContextPool = dbContextPool;
            IsCaching = GetType().Name.EndsWith("Proxy");
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
            Invalidate(user, existingUser == null);
        }

        public virtual async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            using var lease = DbContextPool.Rent();
            var dbContext = lease.Item;
            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            Invalidate(user, false);
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
                Invalidate(user);
                return true;
            }
            catch (DbUpdateConcurrencyException) {
                return false;
            }
        }

        public virtual async Task<User?> TryGetAsync(long userId, CancellationToken cancellationToken = default)
        {
            // Debug.WriteLine($"TryGetAsync {userId}");
            await Everything().ConfigureAwait(false);
            using var lease = DbContextPool.Rent();
            var dbContext = lease.Item;
            var user = await dbContext.Users
                .FindAsync(new[] {(object) userId}, cancellationToken)
                .ConfigureAwait(false);
            user?.Freeze();
            return user;
        }

        [Computed(KeepAliveTime = 5)]
        public virtual async Task<long> CountAsync(CancellationToken cancellationToken = default) 
        {
            await Everything().ConfigureAwait(false);
            using var lease = DbContextPool.Rent();
            var dbContext = lease.Item;
            var count = await dbContext.Users.LongCountAsync(cancellationToken).ConfigureAwait(false);
            Log.LogDebug($"Users.Count query: {count}");
            return count;
        }

        // Change handling

        protected virtual Task<Unit> Everything() => TaskEx.FromUnit();

        public virtual void Invalidate()
        {
            Computed.Invalidate(Everything);
            Log.LogDebug($"Invalidated everything.");
        }

        protected virtual void Invalidate(User user, bool countChanged = true)
        {
            if (!IsCaching)
                return;
            var cUser = Computed.Invalidate(() => TryGetAsync(user.Id)); 
            if (cUser != null)
                Log.LogDebug($"Invalidated: User.Id={user.Id}");
            if (countChanged) {
                var cCount = Computed.Invalidate(() => CountAsync());
                if (cCount != null)
                    Log.LogDebug($"Invalidated: Users.Count");
            }
        }
    }
}
