using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Async;
using Stl.CommandR;
using Stl.CommandR.Commands;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;
using Stl.Fusion.Authentication.Internal;
using Stl.Fusion.Operations;

namespace Stl.Fusion.EntityFramework.Authentication
{
    public class DbAuthService<TDbContext> : DbServiceBase<TDbContext>, IServerSideAuthService
        where TDbContext : DbContext
    {
        public class Options
        {
            public TimeSpan MinUpdatePresencePeriod { get; set; } = TimeSpan.FromMinutes(3);
        }

        protected IDbUserBackend<TDbContext> Users { get; }
        protected IDbSessionInfoBackend<TDbContext> Sessions { get; }
        protected TimeSpan MinUpdatePresencePeriod { get; }

        public DbAuthService(Options options, IServiceProvider services) : base(services)
        {
            MinUpdatePresencePeriod = options.MinUpdatePresencePeriod;
            Users = services.GetRequiredService<IDbUserBackend<TDbContext>>();
            Sessions = services.GetRequiredService<IDbSessionInfoBackend<TDbContext>>();
        }

        // Commands

        public virtual async Task SignInAsync(SignInCommand command, CancellationToken cancellationToken = default)
        {
            var (user, session) = command;
            var context = CommandContext.GetCurrent();
            DbUser dbUser;
            if (Computed.IsInvalidating()) {
                GetUserAsync(session, default).Ignore();
                dbUser = context.Items.Get<OperationItem<DbUser>>().Value;
                GetUserSessionsAsync(dbUser.Id, default).Ignore();
                return;
            }

            if (await IsSignOutForcedAsync(session, cancellationToken).ConfigureAwait(false))
                throw Errors.ForcedSignOut();

            await using var dbContext = await CreateCommandDbContextAsync(cancellationToken).ConfigureAwait(false);
            dbUser = await Users
                .CreateOrUpdateAsync(dbContext, user, cancellationToken)
                .ConfigureAwait(false);
            var dbSessionInfo = await Sessions
                .CreateOrUpdateAsync(dbContext, session.Id, dbUser.Id, false, cancellationToken)
                .ConfigureAwait(false);
            context.Items.Set(OperationItem.New(dbUser));
            context.Items.Set(OperationItem.New(dbSessionInfo));
        }

        public virtual async Task SignOutAsync(SignOutCommand command, CancellationToken cancellationToken = default)
        {
            var (force, session) = command;
            var context = CommandContext.GetCurrent();
            DbSessionInfo dbSessionInfo;
            if (Computed.IsInvalidating()) {
                GetUserAsync(session, default).Ignore();
                if (force)
                    IsSignOutForcedAsync(session, default).Ignore();
                dbSessionInfo = context.Items.Get<OperationItem<DbSessionInfo>>().Value;
                if (dbSessionInfo.UserId.HasValue)
                    GetUserSessionsAsync(dbSessionInfo.UserId.Value, default).Ignore();
                return;
            }

            await using var dbContext = await CreateCommandDbContextAsync(cancellationToken).ConfigureAwait(false);
            dbSessionInfo = await Sessions
                .CreateOrUpdateAsync(dbContext, session.Id, null, force, cancellationToken)
                .ConfigureAwait(false);
            context.Items.Set(OperationItem.New(dbSessionInfo));
        }

        public virtual async Task SaveSessionInfoAsync(SaveSessionInfoCommand command, CancellationToken cancellationToken = default)
        {
            var (sessionInfo, session) = command;
            if (Computed.IsInvalidating()) {
                GetSessionInfoAsync(session, default).Ignore();
                return;
            }

            if (sessionInfo.Id != session.Id)
                throw new ArgumentOutOfRangeException(nameof(command));
            var now = Clock.Now.ToDateTime();
            sessionInfo = sessionInfo with { LastSeenAt = now };

            await using var dbContext = await CreateCommandDbContextAsync(cancellationToken).ConfigureAwait(false);
            await Sessions
                .CreateOrUpdateAsync(dbContext, sessionInfo, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task UpdatePresenceAsync(Session session, CancellationToken cancellationToken = default)
        {
            var sessionInfo = await GetSessionInfoAsync(session, cancellationToken).ConfigureAwait(false);
            var now = Clock.Now.ToDateTime();
            var delta = now - sessionInfo.LastSeenAt;
            if (delta < MinUpdatePresencePeriod)
                return; // We don't want to update this too frequently
            sessionInfo = sessionInfo with { LastSeenAt = now };
            var command = new SaveSessionInfoCommand(sessionInfo, session).MarkServerSide();
            await SaveSessionInfoAsync(command, cancellationToken).ConfigureAwait(false);
        }

        // Compute methods

        public virtual async Task<bool> IsSignOutForcedAsync(Session session, CancellationToken cancellationToken = default)
        {
            await using var dbContext = CreateDbContext();
            var dbSession = await Sessions.FindAsync(dbContext, session.Id, cancellationToken).ConfigureAwait(false);
            return dbSession?.IsSignOutForced == true;
        }

        public virtual async Task<User> GetUserAsync(Session session, CancellationToken cancellationToken = default)
        {
            if (await IsSignOutForcedAsync(session, cancellationToken).ConfigureAwait(false))
                return new User(session.Id);

            await using var dbContext = CreateDbContext();
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var dbSession = await Sessions.FindAsync(dbContext, session.Id, cancellationToken).ConfigureAwait(false);
            if (dbSession?.UserId == null || dbSession.IsSignOutForced)
                return Users.CreateGuestUser(session.Id);
            var dbUser = await Users.FindAsync(dbContext, dbSession.UserId.Value, cancellationToken).ConfigureAwait(false);
            if (dbUser == null)
                return Users.CreateGuestUser(session.Id);
            return await Users.FromDbEntityAsync(dbContext, dbUser, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<SessionInfo> GetSessionInfoAsync(
            Session session, CancellationToken cancellationToken = default)
        {
            await using var dbContext = CreateDbContext();
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var dbSession = await Sessions.FindAsync(dbContext, session.Id, cancellationToken).ConfigureAwait(false);
            if (dbSession == null)
                return Sessions.CreateGuestSessionInfo(session.Id);
            return await Sessions.FromDbEntityAsync(dbContext, dbSession, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<SessionInfo[]> GetUserSessionsAsync(
            Session session, CancellationToken cancellationToken = default)
        {
            var user = await GetUserAsync(session, cancellationToken).ConfigureAwait(false);
            if (!(user.IsAuthenticated && long.TryParse(user.Id, out var userId)))
                return Array.Empty<SessionInfo>();
            return await GetUserSessionsAsync(userId, cancellationToken).ConfigureAwait(false);
        }

        // Protected methods

        [ComputeMethod]
        protected virtual async Task<SessionInfo[]> GetUserSessionsAsync(
            long userId, CancellationToken cancellationToken = default)
        {
            await using var dbContext = CreateDbContext();
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var dbSessions = await Sessions.ListByUserAsync(dbContext, userId, cancellationToken).ConfigureAwait(false);
            var sessions = new SessionInfo[dbSessions.Length];
            for (var i = 0; i < dbSessions.Length; i++)
                sessions[i] = await Sessions
                    .FromDbEntityAsync(dbContext, dbSessions[i], cancellationToken)
                    .ConfigureAwait(false);
            return sessions;
        }
    }
}
