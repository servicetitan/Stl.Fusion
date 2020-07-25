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
using Stl.Tests.Fusion.Model;

namespace Stl.Tests.Fusion.Services
{
    public interface IUserService
    {
        Task CreateAsync(User user, bool orUpdate = false, CancellationToken cancellationToken = default);
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(User user, CancellationToken cancellationToken = default);
        Task<User?> TryGetAsync(long userId, CancellationToken cancellationToken = default);
        Task<long> CountAsync(CancellationToken cancellationToken = default);
        void Invalidate();
    }

    public class UserService : IUserService, IComputedService
    {
        private readonly ILogger _log;
        protected TestDbContextPool DbContextPool { get; }
        protected bool IsCaching { get; }

        public UserService(
            TestDbContextPool dbContextPool,
            ILogger<UserService>? log = null)
        {
            _log = log ??= NullLogger<UserService>.Instance;
            DbContextPool = dbContextPool;
            IsCaching = GetType().Name.EndsWith("Proxy");
        }

        public virtual async Task CreateAsync(User user, bool orUpdate = false, CancellationToken cancellationToken = default)
        {
            using var lease = DbContextPool.Rent();
            var dbContext = lease.Subject;
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
            var dbContext = lease.Subject;
            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            Invalidate(user, false);
        }

        public virtual async Task<bool> DeleteAsync(User user, CancellationToken cancellationToken = default)
        {
            Computed.GetCurrent().Should().BeNull();
            using var lease = DbContextPool.Rent();
            var dbContext = lease.Subject;
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

        [ComputedServiceMethod]
        public virtual async Task<User?> TryGetAsync(long userId, CancellationToken cancellationToken = default)
        {
            // Debug.WriteLine($"TryGetAsync {userId}");
            await Everything().ConfigureAwait(false);
            using var lease = DbContextPool.Rent();
            var dbContext = lease.Subject;
            var user = await dbContext.Users
                .FindAsync(new[] {(object) userId}, cancellationToken)
                .ConfigureAwait(false);
            return user;
        }

        [ComputedServiceMethod(KeepAliveTime = 5)]
        public virtual async Task<long> CountAsync(CancellationToken cancellationToken = default)
        {
            await Everything().ConfigureAwait(false);
            using var lease = DbContextPool.Rent();
            var dbContext = lease.Subject;
            var count = await dbContext.Users.LongCountAsync(cancellationToken).ConfigureAwait(false);
            _log.LogDebug($"Users.Count query: {count}");
            return count;
        }

        // Change handling

        [ComputedServiceMethod]
        protected virtual Task<Unit> Everything() => TaskEx.UnitTask;

        public virtual void Invalidate()
        {
            Computed.Invalidate(Everything);
            _log.LogDebug($"Invalidated everything.");
        }

        protected virtual void Invalidate(User user, bool countChanged = true)
        {
            if (!IsCaching)
                return;
            var cUser = Computed.Invalidate(() => TryGetAsync(user.Id));
            if (cUser != null)
                _log.LogDebug($"Invalidated: User.Id={user.Id}");
            if (countChanged) {
                var cCount = Computed.Invalidate(() => CountAsync());
                if (cCount != null)
                    _log.LogDebug($"Invalidated: Users.Count");
            }
        }
    }
}
