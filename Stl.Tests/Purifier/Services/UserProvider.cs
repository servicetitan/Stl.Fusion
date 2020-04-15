using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Purifier;
using Stl.Purifier.Autofac;
using Stl.Tests.Purifier.Model;

namespace Stl.Tests.Purifier.Services
{
    public interface IUserProvider
    {
        Task CreateAsync(User user);
        Task UpdateAsync(User user);
        Task<bool> DeleteAsync(User user);
        ValueTask<User?> TryGetAsync(long userId);
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

        [Computed(false)]
        public async Task CreateAsync(User user)
        {
            using var lease = DbContextPool.Rent();
            var dbContext = lease.Item;
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        [Computed(false)]
        public async Task UpdateAsync(User user)
        {
            using var lease = DbContextPool.Rent();
            var dbContext = lease.Item;
            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        [Computed(false)]
        public async Task<bool> DeleteAsync(User user)
        {
            Computed.UntypedCurrent.Should().BeNull();
            using var lease = DbContextPool.Rent();
            var dbContext = lease.Item;
            dbContext.Users.Remove(user);
            try {
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
                return true;
            }
            catch (DbUpdateConcurrencyException e) {
                return false;
            }
        }

        public async ValueTask<User?> TryGetAsync(long userId)
        {
            using var lease = DbContextPool.Rent();
            var dbContext = lease.Item;
            return await dbContext.Users.FindAsync(userId).ConfigureAwait(false);
        }
    }
}
