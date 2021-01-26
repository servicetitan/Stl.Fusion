using System;
using System.Linq;
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
    // [ComputeService]
    // [ServiceAlias(typeof(IServerSideAuthService), typeof(AppAuthService))]
    public class AuthService<TDbContext> : DbServiceBase<TDbContext>, IServerSideAuthService
        where TDbContext : DbContext
    {
        protected IAuthServiceBackend<TDbContext> Backend { get; }

        public AuthService(IServiceProvider services) : base(services)
            => Backend = services.GetRequiredService<IAuthServiceBackend<TDbContext>>();

        // Commands

        public virtual async Task SignInAsync(SignInCommand command, CancellationToken cancellationToken = default)
        {
            var (user, session) = command;
            var context = CommandContext.GetCurrent();
            if (Computed.IsInvalidating()) {
                var userId = context.Items.TryGet<OperationItem<long?>>()?.Value;
                GetUserAsync(session, default).Ignore();
                if (userId.HasValue)
                    GetUserSessionsAsync(userId.Value, default).Ignore();
                return;
            }

            if (await IsSignOutForcedAsync(session, cancellationToken).ConfigureAwait(false))
                throw Errors.ForcedSignOut();

            await using var dbContext = await CreateCommandDbContextAsync(cancellationToken).ConfigureAwait(false);

            var (dbUser, _) = await Backend.GetOrCreateDbUserAsync(dbContext, user, cancellationToken).ConfigureAwait(false);
            var dbSession = await Backend.GetOrCreateDbSessionAsync(dbContext, session, cancellationToken).ConfigureAwait(false);
            if (dbSession.UserId != dbUser.Id) {
                dbSession.UserId = dbUser.Id;
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            context.Items.Set(OperationItem.New(dbSession.UserId));
        }

        public virtual async Task SignOutAsync(SignOutCommand command, CancellationToken cancellationToken = default)
        {
            var (force, session) = command;
            var context = CommandContext.GetCurrent();
            if (Computed.IsInvalidating()) {
                var userId = context.Items.TryGet<OperationItem<long?>>()?.Value;
                GetUserAsync(session, default).Ignore();
                if (userId.HasValue)
                    GetUserSessionsAsync(userId.Value, default).Ignore();
                if (force)
                    IsSignOutForcedAsync(session, default).Ignore();
                return;
            }

            await using var dbContext = await CreateCommandDbContextAsync(cancellationToken).ConfigureAwait(false);

            var dbSession = await Backend.GetOrCreateDbSessionAsync(dbContext, session, cancellationToken).ConfigureAwait(false);
            dbSession.IsSignOutForced = force;
            await dbContext.SaveChangesAsync(cancellationToken);
            context.Items.Set(OperationItem.New(dbSession.UserId));
        }

        public virtual async Task SaveSessionInfoAsync(SaveSessionInfoCommand command, CancellationToken cancellationToken = default)
        {
            var (sessionInfo, session) = command;
            if (Computed.IsInvalidating()) {
                GetSessionInfoAsync(session, default).Ignore();
                return;
            }

            if (sessionInfo.Id != session.Id)
                throw new ArgumentOutOfRangeException(nameof(sessionInfo));
            var now = Clock.Now.ToDateTime();
            sessionInfo = sessionInfo with { LastSeenAt = now };

            await using var dbContext = await CreateCommandDbContextAsync(cancellationToken).ConfigureAwait(false);

            var dbSession = await Backend.GetOrCreateDbSessionAsync(dbContext, session, cancellationToken).ConfigureAwait(false);
            dbSession.LastSeenAt = sessionInfo.LastSeenAt;
            dbSession.IPAddress = sessionInfo.IPAddress;
            dbSession.UserAgent = sessionInfo.UserAgent;
            dbSession.ExtraPropertiesJson = Backend.ToJson(sessionInfo.ExtraProperties!.ToDictionary(kv => kv.Key, kv => kv.Value));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdatePresenceAsync(Session session, CancellationToken cancellationToken = default)
        {
            var sessionInfo = await GetSessionInfoAsync(session, cancellationToken).ConfigureAwait(false);
            var now = Clock.Now.ToDateTime();
            var delta = now - sessionInfo.LastSeenAt;
            if (delta < TimeSpan.FromMinutes(3))
                return; // We don't want to update this too frequently
            sessionInfo = sessionInfo with { LastSeenAt = now };
            var command = new SaveSessionInfoCommand(sessionInfo, session).MarkServerSide();
            await SaveSessionInfoAsync(command, cancellationToken).ConfigureAwait(false);
        }

        // Compute methods

        public virtual async Task<bool> IsSignOutForcedAsync(Session session, CancellationToken cancellationToken = default)
        {
            await using var dbContext = CreateDbContext();
            var dbSession = await Backend.TryGetDbSessionAsync(dbContext, session.Id, cancellationToken).ConfigureAwait(false);
            return dbSession?.IsSignOutForced == true;
        }

        public virtual async Task<User> GetUserAsync(Session session, CancellationToken cancellationToken = default)
        {
            if (await IsSignOutForcedAsync(session, cancellationToken).ConfigureAwait(false))
                return new User(session.Id);

            await using var dbContext = CreateDbContext();
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var dbSession = await Backend.TryGetDbSessionAsync(dbContext, session.Id, cancellationToken).ConfigureAwait(false);
            if (dbSession?.UserId == null || dbSession.IsSignOutForced)
                return Backend.CreateAnonymousUser(session.Id);
            var dbUser = await Backend.TryGetDbUserAsync(dbContext, dbSession.UserId.Value, cancellationToken).ConfigureAwait(false);
            if (dbUser == null)
                return Backend.CreateAnonymousUser(session.Id);
            return await Backend.FromDbEntityAsync(dbContext, dbUser, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<SessionInfo> GetSessionInfoAsync(
            Session session, CancellationToken cancellationToken = default)
        {
            await using var dbContext = CreateDbContext();
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var dbSession = await Backend.TryGetDbSessionAsync(dbContext, session.Id, cancellationToken).ConfigureAwait(false);
            if (dbSession == null)
                return Backend.CreateSession(session.Id);
            return await Backend.FromDbEntityAsync(dbContext, dbSession, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<SessionInfo[]> GetUserSessionsAsync(
            Session session, CancellationToken cancellationToken = default)
        {
            var user = await GetUserAsync(session, cancellationToken).ConfigureAwait(false);
            if (!(user.IsAuthenticated && long.TryParse(user.Id, out var userId)))
                return Array.Empty<SessionInfo>();
            return await GetUserSessionsAsync(userId, cancellationToken).ConfigureAwait(false);
        }

        [ComputeMethod]
        protected virtual async Task<SessionInfo[]> GetUserSessionsAsync(
            long userId, CancellationToken cancellationToken = default)
        {
            await using var dbContext = CreateDbContext();
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var dbSessions = await Backend.GetUserDbSessionsAsync(dbContext, userId, cancellationToken).ConfigureAwait(false);
            var sessions = new SessionInfo[dbSessions.Length];
            for (var i = 0; i < dbSessions.Length; i++)
                sessions[i] = await Backend
                    .FromDbEntityAsync(dbContext, dbSessions[i], cancellationToken)
                    .ConfigureAwait(false);
            return sessions;
        }
    }
}
