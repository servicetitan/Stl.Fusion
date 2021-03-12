using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Stl.Async;
using Stl.CommandR;
using Stl.CommandR.Configuration;
using Stl.DependencyInjection;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.Operations;
using Stl.Fusion.Tests.Model;

namespace Stl.Fusion.Tests.Services
{
    public interface IUserService
    {
        public record AddCommand(User User, bool OrUpdate = false) : ICommand<Unit>
        {
            public AddCommand() : this(null!, false) { }
        }
        public record UpdateCommand(User User) : ICommand<Unit>
        {
            public UpdateCommand() : this(default(User)!) { }
        }
        public record DeleteCommand(User User) : ICommand<bool>
        {
            public DeleteCommand() : this(default(User)!) { }
        }

        [CommandHandler]
        Task Create(AddCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task Update(UpdateCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task<bool> Delete(DeleteCommand command, CancellationToken cancellationToken = default);

        [ComputeMethod(KeepAliveTime = 1)]
        Task<User?> TryGet(long userId, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 1)]
        Task<long> Count(CancellationToken cancellationToken = default);
        void Invalidate();
    }

    [ComputeService(typeof(IUserService), Scope = ServiceScope.Services)] // Fusion version
    [Service] // "No Fusion" version
    public class UserService : DbServiceBase<TestDbContext>, IUserService
    {
        protected bool IsProxy { get; }

        public UserService(IServiceProvider services) : base(services)
        {
            IsProxy = GetType().Name.EndsWith("Proxy");
        }

        public virtual async Task Create(IUserService.AddCommand command, CancellationToken cancellationToken = default)
        {
            var (user, orUpdate) = command;
            var existingUser = (User?) null;
            var context = CommandContext.GetCurrent();
            if (Computed.IsInvalidating()) {
                existingUser = context.Operation().Items.TryGet<User>();
                TryGet(user.Id, default).AssertCompleted().Ignore();
                if (existingUser == null)
                    Count(default).AssertCompleted().Ignore();
                return;
            }

            await using var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
            dbContext.DisableChangeTracking();
            var userId = user.Id;
            if (orUpdate) {
                existingUser = await dbContext.Users.FindAsync(new [] {(object) userId}, cancellationToken);
                context.Operation().Items.Set(existingUser);
                if (existingUser != null)
                    dbContext.Users.Update(user);
            }
            if (existingUser == null)
                dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task Update(IUserService.UpdateCommand command, CancellationToken cancellationToken = default)
        {
            var user = command.User;
            if (Computed.IsInvalidating()) {
                TryGet(user.Id, default).AssertCompleted().Ignore();
                return;
            }

            await using var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<bool> Delete(IUserService.DeleteCommand command, CancellationToken cancellationToken = default)
        {
            var user = command.User;
            var context = CommandContext.GetCurrent();
            if (Computed.IsInvalidating()) {
                var success = context.Operation().Items.TryGet<bool>();
                if (success) {
                    TryGet(user.Id, default).AssertCompleted().Ignore();
                    Count(default).AssertCompleted().Ignore();
                }
                return false;
            }

            await using var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
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

        public virtual async Task<User?> TryGet(long userId, CancellationToken cancellationToken = default)
        {
            // Debug.WriteLine($"TryGetAsync {userId}");
            await Everything().ConfigureAwait(false);
            await using var dbContext = DbContextFactory.CreateDbContext();
            var user = await dbContext.Users
                .FindAsync(new[] {(object) userId}, cancellationToken)
                .ConfigureAwait(false);
            return user;
        }

        public virtual async Task<long> Count(CancellationToken cancellationToken = default)
        {
            await Everything().ConfigureAwait(false);
            await using var dbContext = DbContextFactory.CreateDbContext();
            var count = await dbContext.Users.LongCountAsync(cancellationToken).ConfigureAwait(false);
            // _log.LogDebug($"Users.Count query: {count}");
            return count;
        }

        public virtual void Invalidate()
        {
            if (!IsProxy)
                return;

            using (Computed.Invalidate()) {
                Everything().AssertCompleted();
            }
        }

        // Protected & private methods

        [ComputeMethod]
        protected virtual Task<Unit> Everything() => TaskEx.UnitTask;

        private new Task<TestDbContext> CreateCommandDbContext(CancellationToken cancellationToken = default)
        {
            if (IsProxy)
                return base.CreateCommandDbContext(cancellationToken);
            return Task.FromResult(CreateDbContext().ReadWrite());
        }
    }
}
