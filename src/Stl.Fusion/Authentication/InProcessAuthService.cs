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

            var authenticatedIdentity = user.Identities.FirstOrDefault().Key;
            if (string.IsNullOrEmpty(user.Id)) {
                var isExistingUser = false;
                if (authenticatedIdentity.IsValid) {
                    // Let's try to find the user by its authenticated identity
                    foreach (var existingUser in Users.Values) {
                        if (existingUser.Identities.ContainsKey(authenticatedIdentity)) {
                            user = existingUser;
                            isExistingUser = true;
                            break;
                        }
                    }
                }
                // Generate User.Id for the new user
                if (!isExistingUser)
                    user = user with { Id = GetNextUserId() };
            }
            else {
                // Just to make sure it works like a similar EF service
                var _ = long.Parse(user.Id);
            }

            // Update SessionInfo
            var sessionInfo = await GetSessionInfoAsync(session, cancellationToken).ConfigureAwait(false);
            sessionInfo = sessionInfo with {
                AuthenticatedIdentity = authenticatedIdentity,
                UserId = user.Id,
            };

            // Persist changes
            Users[user.Id] = user;
            sessionInfo = AddOrUpdateSessionInfo(sessionInfo);
            context.Items.Set(OperationItem.New(sessionInfo));
        }

        public virtual async Task SignOutAsync(SignOutCommand command, CancellationToken cancellationToken = default)
        {
            var (force, session) = command;
            var context = CommandContext.GetCurrent();
            if (Computed.IsInvalidating()) {
                if (force)
                    IsSignOutForcedAsync(session, default).Ignore();
                GetSessionInfoAsync(session, default).Ignore();
                var invSessionInfo = context.Items.Get<OperationItem<SessionInfo>>().Value;
                TryGetUserAsync(invSessionInfo.UserId, default).Ignore();
                GetUserSessionsAsync(invSessionInfo.UserId, default).Ignore();
                return;
            }

            var sessionInfo = await GetSessionInfoAsync(session, cancellationToken).ConfigureAwait(false);
            context.Items.Set(OperationItem.New(sessionInfo));

            // Updating SessionInfo
            sessionInfo = sessionInfo with {
                AuthenticatedIdentity = "",
                UserId = "",
                IsSignOutForced = force,
            };
            AddOrUpdateSessionInfo(sessionInfo);
        }

        public virtual async Task<SessionInfo> SetupSessionAsync(SetupSessionCommand command, CancellationToken cancellationToken = default)
        {
            var (ipAddress, userAgent, session) = command;
            var context = CommandContext.GetCurrent();
            if (Computed.IsInvalidating()) {
                GetSessionInfoAsync(session, default).Ignore();
                var invSessionInfo = context.Items.Get<OperationItem<SessionInfo>>().Value;
                if (invSessionInfo.HasUser)
                    GetUserSessionsAsync(invSessionInfo.UserId, default).Ignore();
                return null!;
            }
            var oldSessionInfo = await GetSessionInfoAsync(session, cancellationToken).ConfigureAwait(false);
            var newSessionInfo = oldSessionInfo with {
                IPAddress = string.IsNullOrEmpty(ipAddress) ? oldSessionInfo.IPAddress : ipAddress,
                UserAgent = string.IsNullOrEmpty(userAgent) ? oldSessionInfo.UserAgent : userAgent,
            };
            newSessionInfo = AddOrUpdateSessionInfo(newSessionInfo);
            context.Items.Set(OperationItem.New(newSessionInfo));
            return newSessionInfo;
        }

        public virtual async Task UpdatePresenceAsync(Session session, CancellationToken cancellationToken = default)
        {
            var sessionInfo = await GetSessionInfoAsync(session, cancellationToken).ConfigureAwait(false);
            var now = Clock.Now.ToDateTime();
            var delta = now - sessionInfo.LastSeenAt;
            if (delta < TimeSpan.FromSeconds(10))
                return; // We don't want to update this too frequently
            var command = new SetupSessionCommand(session).MarkServerSide();
            await SetupSessionAsync(command, cancellationToken).ConfigureAwait(false);
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
            if (sessionInfo.IsSignOutForced || !sessionInfo.HasUser)
                return new User(session.Id);
            var user = await TryGetUserAsync(sessionInfo.UserId, cancellationToken).ConfigureAwait(false);
            return (user ?? new User(session.Id)).ToClientSideUser();
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

        protected virtual SessionInfo AddOrUpdateSessionInfo(SessionInfo sessionInfo)
        {
            sessionInfo = sessionInfo with { LastSeenAt = Clock.Now };
            SessionInfos.AddOrUpdate(sessionInfo.Id, sessionInfo, (sessionId, oldSessionInfo) => {
                if (oldSessionInfo.IsSignOutForced)
                    throw Errors.ForcedSignOut();
                return sessionInfo.CreatedAt == oldSessionInfo.CreatedAt
                    ? sessionInfo
                    : sessionInfo with { CreatedAt = oldSessionInfo.CreatedAt };
            });
            return SessionInfos.GetValueOrDefault(sessionInfo.Id) ?? sessionInfo;
        }

        protected string GetNextUserId()
            => Interlocked.Increment(ref _nextUserId).ToString();
    }
}
