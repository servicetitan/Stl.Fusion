using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.CommandR;
using Stl.CommandR.Commands;
using Stl.Fusion.Authentication.Commands;
using Stl.Fusion.Authentication.Internal;
using Stl.Fusion.Operations;
using Stl.Time;

namespace Stl.Fusion.Authentication
{
    public class InProcessAuthService : IServerSideAuthService
    {
        private long _nextUserId;
        protected ConcurrentDictionary<string, User> Users { get; } = new();
        protected ConcurrentDictionary<string, SessionInfo> SessionInfos { get; } = new();
        protected IMomentClock Clock { get; }

        public InProcessAuthService(IMomentClock clock)
            => Clock = clock;

        // Command handlers

        public virtual async Task SignInAsync(SignInCommand command, CancellationToken cancellationToken = default)
        {
            var (user, session) = command;
            var context = CommandContext.GetCurrent();
            if (Computed.IsInvalidating()) {
                GetSessionInfoAsync(session, default).Ignore();
                var invSessionInfo = context.Items.Get<OperationItem<SessionInfo>>().Value;
                TryGetUserAsync(invSessionInfo.UserId, default).Ignore();
                GetUserSessionsAsync(invSessionInfo.UserId, default).Ignore();
                return;
            }

            if (await IsSignOutForcedAsync(session, cancellationToken).ConfigureAwait(false))
                throw Errors.ForcedSignOut();

            // Generate User.Id
            if (string.IsNullOrEmpty(user.Id))
                user = user with { Id = GetNextUserId() };

            // Update SessionInfo
            var sessionInfo = await GetSessionInfoAsync(session, cancellationToken).ConfigureAwait(false);
            sessionInfo = sessionInfo with {
                AuthenticatedAs = user.Identities.FirstOrDefault().Key,
                UserId = user.Id,
            };
            context.Items.Set(OperationItem.New(sessionInfo));

            // Persist changes
            Users[user.Id] = user;
            await SaveSessionInfoAsync(new(sessionInfo, session), cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task SignOutAsync(SignOutCommand command, CancellationToken cancellationToken = default)
        {
            var (force, session) = command;
            var context = CommandContext.GetCurrent();
            if (Computed.IsInvalidating()) {
                if (force)
                    IsSignOutForcedAsync(session, default).Ignore();
                GetSessionInfoAsync(session, default).Ignore();
                var invUser = context.Items.Get<OperationItem<User>>().Value;
                TryGetUserAsync(invUser.Id, default).Ignore();
                GetUserSessionsAsync(invUser.Id, default).Ignore();
                return;
            }

            var sessionInfo = await GetSessionInfoAsync(session, cancellationToken).ConfigureAwait(false);
            context.Items.Set(OperationItem.New(sessionInfo));
            var user = await TryGetUserAsync(session.Id, cancellationToken).ConfigureAwait(false);
            context.Items.Set(OperationItem.New(user));

            // Updating SessionInfo
            sessionInfo = sessionInfo with {
                AuthenticatedAs = "",
                UserId = "",
                IsSignOutForced = force,
            };
            await SaveSessionInfoAsync(new(sessionInfo, session), cancellationToken).ConfigureAwait(false);
        }

        public virtual Task SaveSessionInfoAsync(SaveSessionInfoCommand command, CancellationToken cancellationToken = default)
        {
            var (sessionInfo, session) = command;
            var context = CommandContext.GetCurrent();
            if (Computed.IsInvalidating()) {
                GetSessionInfoAsync(session, default).Ignore();
                var invSessionInfo = context.Items.Get<OperationItem<SessionInfo>>().Value;
                if (invSessionInfo.IsAuthenticated)
                    GetUserSessionsAsync(sessionInfo.UserId, default).Ignore();
                return Task.CompletedTask;
            }
            if (sessionInfo.Id != session.Id)
                throw new ArgumentOutOfRangeException(nameof(sessionInfo));

            var now = Clock.Now.ToDateTime();
            sessionInfo = sessionInfo with { LastSeenAt = now };
            SessionInfos.AddOrUpdate(session.Id, sessionInfo, (sessionId, oldSessionInfo) =>
                sessionInfo.CreatedAt == oldSessionInfo.CreatedAt
                ? sessionInfo
                : sessionInfo with { CreatedAt = oldSessionInfo.CreatedAt });
            context.Items.Set(OperationItem.New(sessionInfo));
            return Task.CompletedTask;
        }

        public virtual async Task UpdatePresenceAsync(Session session, CancellationToken cancellationToken = default)
        {
            var sessionInfo = await GetSessionInfoAsync(session, cancellationToken).ConfigureAwait(false);
            var now = Clock.Now.ToDateTime();
            var delta = now - sessionInfo.LastSeenAt;
            if (delta < TimeSpan.FromSeconds(10))
                return; // We don't want to update this too frequently
            sessionInfo = sessionInfo with { LastSeenAt = now };
            var command = new SaveSessionInfoCommand(sessionInfo, session).MarkServerSide();
            await SaveSessionInfoAsync(command, cancellationToken).ConfigureAwait(false);
        }

        // Compute methods

        public virtual async Task<bool> IsSignOutForcedAsync(Session session, CancellationToken cancellationToken = default)
        {
            var sessionInfo = await GetSessionInfoAsync(session, cancellationToken).ConfigureAwait(false);
            return sessionInfo.IsSignOutForced;
        }

        public virtual Task<SessionInfo> GetSessionInfoAsync(
            Session session, CancellationToken cancellationToken = default)
        {
            var sessionInfo = SessionInfos.GetValueOrDefault(session);
            sessionInfo = sessionInfo.OrDefault(session, Clock); // To mask signed out sessions
            return Task.FromResult(sessionInfo);
        }

        public virtual async Task<User> GetUserAsync(Session session, CancellationToken cancellationToken = default)
        {
            var sessionInfo = await GetSessionInfoAsync(session, cancellationToken).ConfigureAwait(false);
            if (sessionInfo.IsSignOutForced || !sessionInfo.IsAuthenticated)
                return new User(session.Id);
            var user = await TryGetUserAsync(sessionInfo.UserId, cancellationToken).ConfigureAwait(false);
            return user ?? new User(session.Id);
        }

        public virtual Task<User?> TryGetUserAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.TryGetValue(userId, out var user) ? user : null);

        public virtual async Task<SessionInfo[]> GetUserSessionsAsync(
            Session session, CancellationToken cancellationToken = default)
        {
            var user = await GetUserAsync(session, cancellationToken).ConfigureAwait(false);
            if (!user.IsAuthenticated)
                return Array.Empty<SessionInfo>();
            return await GetUserSessionsAsync(user.Id, cancellationToken).ConfigureAwait(false);
        }

        // Protected methods

        [ComputeMethod]
        protected virtual Task<SessionInfo[]> GetUserSessionsAsync(
            string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(userId))
                return Task.FromResult(Array.Empty<SessionInfo>());
            var result = SessionInfos.Values
                .Where(si => si.UserId == userId)
                .OrderByDescending(si => si.LastSeenAt)
                .ToArray();
            return Task.FromResult(result);
        }

        protected string GetNextUserId()
            => Interlocked.Increment(ref _nextUserId).ToString();
    }
}
