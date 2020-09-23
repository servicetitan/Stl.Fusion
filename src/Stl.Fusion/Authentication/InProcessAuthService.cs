using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Concurrency;

namespace Stl.Fusion.Authentication
{
    public class InProcessAuthService : IServerSideAuthService
    {
        protected ConcurrentDictionary<string, ImmutableHashSet<string>> UserSessions { get; } =
            new ConcurrentDictionary<string, ImmutableHashSet<string>>();
        protected ConcurrentDictionary<string, SessionInfo> SessionInfos { get; } =
            new ConcurrentDictionary<string, SessionInfo>();
        protected ConcurrentDictionary<string, User> Users { get; } =
            new ConcurrentDictionary<string, User>();

        public Task LoginAsync(
            User user, Session? session = null,
            CancellationToken cancellationToken = default)
        {
            session ??= Session.Current.AssertNotNull();
            Users[session.Id] = user;
            UserSessions.AddOrUpdate(user.Id,
                (userId, sessionId) => ImmutableHashSet<string>.Empty.Add(sessionId),
                (userId, sessionIds, sessionId) => sessionIds.Add(sessionId),
                session.Id);
            Computed.Invalidate(() => GetUserAsync(session, default));
            Computed.Invalidate(() => GetFakeUserInfo(user.Id));
            return Task.CompletedTask;

        }

        public Task LogoutAsync(
            Session? session = null,
            CancellationToken cancellationToken = default)
        {
            session ??= Session.Current.AssertNotNull();
            if (Users.TryRemove(session.Id, out var user)) {
                UserSessions.AddOrUpdate(user.Id,
                    (userId, sessionId) => ImmutableHashSet<string>.Empty,
                    (userId, sessionIds, sessionId) => sessionIds.Remove(sessionId),
                    session.Id);
                UserSessions.TryRemove(user.Id, ImmutableHashSet<string>.Empty); // No need to store an empty one
                Computed.Invalidate(() => GetUserAsync(session, default));
                Computed.Invalidate(() => GetFakeUserInfo(user.Id));
            }
            return Task.CompletedTask;
        }

        public Task SaveSessionInfoAsync(SessionInfo sessionInfo, Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= Session.Current.AssertNotNull();
            if (sessionInfo.Id != session.Id)
                throw new ArgumentOutOfRangeException(nameof(sessionInfo));
            var now = DateTime.UtcNow;
            sessionInfo.CreatedAt ??= now;
            sessionInfo.LastSeenAt ??= now;
            SessionInfos.AddOrUpdate(session.Id, sessionInfo, (sessionId, oldSessionInfo) => {
                if (oldSessionInfo.CreatedAt.HasValue)
                    sessionInfo.CreatedAt = oldSessionInfo.CreatedAt;
                return sessionInfo;
            });
            Computed.Invalidate(() => GetSessionInfoAsync(session, default));
            return Task.CompletedTask;
        }

        public async Task UpdatePresenceAsync(Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= Session.Current.AssertNotNull();
            var sessionInfo = await GetSessionInfoAsync(session, cancellationToken).ConfigureAwait(false);
            var now = DateTime.UtcNow;
            sessionInfo.LastSeenAt = now;
            await SaveSessionInfoAsync(sessionInfo, session, cancellationToken).ConfigureAwait(false);
        }

        // Compute methods

        public virtual Task<User> GetUserAsync(
            Session? session = null,
            CancellationToken cancellationToken = default)
        {
            session ??= Session.Current.AssertNotNull();
            var user = Users.GetValueOrDefault(session.Id) ?? new User(session.Id);
            return Task.FromResult(user)!;
        }

        public virtual Task<SessionInfo> GetSessionInfoAsync(Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= Session.Current.AssertNotNull();
            var sessionInfo = SessionInfos.GetValueOrDefault(session.Id) ?? new SessionInfo(session.Id);
            return Task.FromResult(sessionInfo)!;
        }

        public virtual async Task<SessionInfo[]> GetUserSessions(Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= Session.Current.AssertNotNull();
            var user = await GetUserAsync(session, cancellationToken).ConfigureAwait(false);
            if (!user.IsAuthenticated)
                return Array.Empty<SessionInfo>();

            await GetFakeUserInfo(user.Id).ConfigureAwait(false);
            var sessionIds = UserSessions.GetValueOrDefault(session.Id) ?? ImmutableHashSet<string>.Empty;
            var result = new List<SessionInfo>();
            foreach (var sessionId in sessionIds) {
                var tmpSession = new Session(sessionId);
                var sessionInfo = await GetSessionInfoAsync(tmpSession, cancellationToken).ConfigureAwait(false);
                result.Add(sessionInfo);
            }
            return result.OrderByDescending(si => si.LastSeenAt ?? DateTime.Now).ToArray();
        }

        [ComputeMethod]
        protected virtual Task<Unit> GetFakeUserInfo(string userId) => TaskEx.UnitTask;
    }
}
