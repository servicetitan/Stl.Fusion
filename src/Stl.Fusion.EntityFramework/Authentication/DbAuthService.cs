using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Async;
using Stl.CommandR;
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
            // The default should be less than 3 min - see PresenceService.Options
            public TimeSpan MinUpdatePresencePeriod { get; set; } = TimeSpan.FromMinutes(2.75);
        }

        protected IDbUserRepo<TDbContext> Users { get; }
        protected IDbSessionInfoRepo<TDbContext> Sessions { get; }
        protected ISessionFactory SessionFactory { get; }
        protected TimeSpan MinUpdatePresencePeriod { get; }

        public DbAuthService(Options? options, IServiceProvider services) : base(services)
        {
            options ??= new();
            MinUpdatePresencePeriod = options.MinUpdatePresencePeriod;
            Users = services.GetRequiredService<IDbUserRepo<TDbContext>>();
            Sessions = services.GetRequiredService<IDbSessionInfoRepo<TDbContext>>();
            SessionFactory = services.GetRequiredService<ISessionFactory>();
        }

        // Commands

        public virtual async Task SignIn(
            SignInCommand command, CancellationToken cancellationToken = default)
        {
            var (session, user, authenticatedIdentity) = command;
            var context = CommandContext.GetCurrent();
            if (Computed.IsInvalidating()) {
                GetSessionInfo(session, default).Ignore();
                var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
                TryGetUser(invSessionInfo.UserId, default).Ignore();
                GetUserSessions(invSessionInfo.UserId, default).Ignore();
                return;
            }

            if (!user.Identities.ContainsKey(authenticatedIdentity))
                throw new ArgumentOutOfRangeException(
                    $"{nameof(command)}.{nameof(SignInCommand.AuthenticatedIdentity)}");
            if (await IsSignOutForced(session, cancellationToken).ConfigureAwait(false))
                throw Errors.ForcedSignOut();

            await using var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);

            var isNewUser = false;
            var dbUser = await Users
                .TryGetByUserIdentity(dbContext, authenticatedIdentity, cancellationToken)
                .ConfigureAwait(false);
            if (dbUser == null) {
                (dbUser, isNewUser) = await Users
                    .GetOrCreateOnSignIn(dbContext, user, cancellationToken)
                    .ConfigureAwait(false);
                if (isNewUser == false) {
                    dbUser.UpdateFrom(user);
                    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            else {
                user = user with { Id = dbUser.Id.ToString() };
                dbUser.UpdateFrom(user);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            var dbSessionInfo = await Sessions.GetOrCreate(dbContext, session, cancellationToken).ConfigureAwait(false);
            var sessionInfo = dbSessionInfo.ToModel();
            if (sessionInfo.IsSignOutForced)
                throw Errors.ForcedSignOut();
            sessionInfo = sessionInfo with {
                LastSeenAt = Clock.Now,
                AuthenticatedIdentity = authenticatedIdentity,
                UserId = dbUser.Id.ToString(),
            };
            context.Operation().Items.Set(sessionInfo);
            context.Operation().Items.Set(isNewUser);
            await Sessions.CreateOrUpdate(dbContext, sessionInfo, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task SignOut(
            SignOutCommand command, CancellationToken cancellationToken = default)
        {
            var (session, force) = command;
            var context = CommandContext.GetCurrent();
            if (Computed.IsInvalidating()) {
                GetSessionInfo(session, default).Ignore();
                var invSessionInfo = context.Operation().Items.TryGet<SessionInfo>();
                if (invSessionInfo != null) {
                    TryGetUser(invSessionInfo.UserId, default).Ignore();
                    GetUserSessions(invSessionInfo.UserId, default).Ignore();
                }
                return;
            }

            await using var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);

            var dbSessionInfo = await Sessions.GetOrCreate(dbContext, session, cancellationToken).ConfigureAwait(false);
            var sessionInfo = dbSessionInfo.ToModel();
            if (sessionInfo.IsSignOutForced)
                return;

            context.Operation().Items.Set(sessionInfo);
            sessionInfo = sessionInfo with {
                LastSeenAt = Clock.Now,
                AuthenticatedIdentity = "",
                UserId = "",
                IsSignOutForced = force,
            };
            await Sessions.CreateOrUpdate(dbContext, sessionInfo, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task EditUser(EditUserCommand command, CancellationToken cancellationToken = default)
        {
            var session = command.Session;
            var context = CommandContext.GetCurrent();
            if (Computed.IsInvalidating()) {
                var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
                TryGetUser(invSessionInfo.UserId, default).Ignore();
                return;
            }

            var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
            if (!sessionInfo.IsAuthenticated)
                throw Errors.NotAuthenticated();

            await using var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);

            var longUserId = long.Parse(sessionInfo.UserId);
            var dbUser = await Users.TryGet(dbContext, longUserId, cancellationToken).ConfigureAwait(false);
            if (dbUser == null)
                throw Internal.Errors.EntityNotFound(Users.UserEntityType);
            await Users.Edit(dbContext, dbUser, command, cancellationToken).ConfigureAwait(false);
            context.Operation().Items.Set(sessionInfo);
        }

        public virtual async Task<SessionInfo> SetupSession(
            SetupSessionCommand command, CancellationToken cancellationToken = default)
        {
            var (session, ipAddress, userAgent) = command;
            var context = CommandContext.GetCurrent();
            if (Computed.IsInvalidating()) {
                GetSessionInfo(session, default).Ignore();
                var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
                if (invSessionInfo.IsAuthenticated)
                    GetUserSessions(invSessionInfo.UserId, default).Ignore();
                return null!;
            }

            await using var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);

            var dbSessionInfo = await Sessions.TryGet(dbContext, session.Id, cancellationToken).ConfigureAwait(false);
            var now = Clock.Now;
            var oldSessionInfo = dbSessionInfo?.ToModel() ?? new SessionInfo(session.Id, now);
            var newSessionInfo = oldSessionInfo with {
                LastSeenAt = now,
                IPAddress = string.IsNullOrEmpty(ipAddress) ? oldSessionInfo.IPAddress : ipAddress,
                UserAgent = string.IsNullOrEmpty(userAgent) ? oldSessionInfo.UserAgent : userAgent,
            };
            dbSessionInfo = await Sessions.CreateOrUpdate(dbContext, newSessionInfo, cancellationToken).ConfigureAwait(false);
            var sessionInfo = dbSessionInfo.ToModel();
            context.Operation().Items.Set(sessionInfo);
            return sessionInfo;
        }

        public virtual async Task UpdatePresence(
            Session session, CancellationToken cancellationToken = default)
        {
            var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
            var now = Clock.Now.ToDateTime();
            var delta = now - sessionInfo.LastSeenAt;
            if (delta < MinUpdatePresencePeriod)
                return; // We don't want to update this too frequently
            var command = new SetupSessionCommand(session).MarkServerSide();
            await SetupSession(command, cancellationToken).ConfigureAwait(false);
        }

        // Compute methods

        public virtual async Task<bool> IsSignOutForced(
            Session session, CancellationToken cancellationToken = default)
        {
            var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
            return sessionInfo.IsSignOutForced;
        }

        public virtual async Task<SessionInfo> GetSessionInfo(
            Session session, CancellationToken cancellationToken = default)
        {
            var dbSessionInfo = await Sessions.TryGet(session.Id, cancellationToken).ConfigureAwait(false);
            if (dbSessionInfo == null)
                return new(session.Id, Clock.Now);
            return dbSessionInfo.ToModel();
        }

        public virtual async Task<User> GetUser(
            Session session, CancellationToken cancellationToken = default)
        {
            var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
            if (sessionInfo.IsSignOutForced || !sessionInfo.IsAuthenticated)
                return new User(session.Id);
            var user = await TryGetUser(sessionInfo.UserId, cancellationToken).ConfigureAwait(false);
            return (user ?? new User(session.Id)).ToClientSideUser();
        }

        public virtual async Task<User?> TryGetUser(
            string userId, CancellationToken cancellationToken = default)
        {
            var dbUser = await Users.TryGet(long.Parse(userId), cancellationToken).ConfigureAwait(false);
            return dbUser?.ToModel();
        }

        public virtual async Task<SessionInfo[]> GetUserSessions(
            Session session, CancellationToken cancellationToken = default)
        {
            var user = await GetUser(session, cancellationToken).ConfigureAwait(false);
            if (!user.IsAuthenticated)
                return Array.Empty<SessionInfo>();
            return await GetUserSessions(user.Id, cancellationToken).ConfigureAwait(false);
        }

        // Non-[ComputeMethod] queries

        public virtual Task<Session> GetSession(CancellationToken cancellationToken = default)
            => Task.FromResult(SessionFactory.CreateSession());

        // Protected methods

        [ComputeMethod]
        protected virtual async Task<SessionInfo[]> GetUserSessions(
            string userId, CancellationToken cancellationToken = default)
        {
            if (!long.TryParse(userId, out var longUserId))
                return Array.Empty<SessionInfo>();

            await using var dbContext = CreateDbContext();
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var dbSessions = await Sessions.ListByUser(dbContext, longUserId, cancellationToken).ConfigureAwait(false);
            var sessions = new SessionInfo[dbSessions.Length];
            for (var i = 0; i < dbSessions.Length; i++)
                sessions[i] = dbSessions[i].ToModel();
            return sessions;
        }
    }
}
