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
using Stl.Fusion.Authentication.Internal;
using Stl.Time;

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
        protected ConcurrentDictionary<string, Unit> ForcedSignOuts { get; } =
            new ConcurrentDictionary<string, Unit>();
        protected IMomentClock Clock { get; }

        public InProcessAuthService(IMomentClock clock)
            => Clock = clock;

        public async Task SignInAsync(User user, Session session, CancellationToken cancellationToken = default)
        {
            if (await IsSignOutForcedAsync(session, cancellationToken).ConfigureAwait(false))
                throw Errors.ForcedSignOut();
            Users[session.Id] = user;
            UserSessions.AddOrUpdate(user.Id,
                (userId, sessionId) => ImmutableHashSet<string>.Empty.Add(sessionId),
                (userId, sessionIds, sessionId) => sessionIds.Add(sessionId),
                session.Id);
            Computed.Invalidate(() => GetUserAsync(session, default));
            Computed.Invalidate(() => GetFakeUserInfo(user.Id));
        }

        public Task SignOutAsync(bool force, Session session, CancellationToken cancellationToken = default)
        {
            if (force && ForcedSignOuts.TryAdd(session.Id, default))
                Computed.Invalidate(() => IsSignOutForcedAsync(session, default));
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

        public Task SaveSessionInfoAsync(SessionInfo sessionInfo, Session session, CancellationToken cancellationToken = default)
        {
            if (sessionInfo.Id != session.Id)
                throw new ArgumentOutOfRangeException(nameof(sessionInfo));
            var now = Clock.Now.ToDateTime();
            sessionInfo.LastSeenAt = now;
            SessionInfos.AddOrUpdate(session.Id, sessionInfo, (sessionId, oldSessionInfo) => {
                sessionInfo.CreatedAt = oldSessionInfo.CreatedAt;
                return sessionInfo;
            });
            Computed.Invalidate(() => GetSessionInfoAsync(session, default));
            return Task.CompletedTask;
        }

        public async Task UpdatePresenceAsync(Session session, CancellationToken cancellationToken = default)
        {
            var sessionInfo = await GetSessionInfoAsync(session, cancellationToken).ConfigureAwait(false);
            var now = Clock.Now.ToDateTime();
            var delta = now - sessionInfo.LastSeenAt;
            if (delta < TimeSpan.FromSeconds(10))
                return; // We don't want to update this too frequently
            sessionInfo.LastSeenAt = now;
            await SaveSessionInfoAsync(sessionInfo, session, cancellationToken).ConfigureAwait(false);
        }

        // Compute methods

        public virtual Task<bool> IsSignOutForcedAsync(Session session, CancellationToken cancellationToken = default)
            => Task.FromResult(ForcedSignOuts.ContainsKey(session.Id));

        public virtual async Task<User> GetUserAsync(Session session, CancellationToken cancellationToken = default)
        {
            if (await IsSignOutForcedAsync(session, cancellationToken).ConfigureAwait(false))
                return new User(session.Id);
            return Users.GetValueOrDefault(session.Id) ?? new User(session.Id);
        }

        public virtual Task<SessionInfo> GetSessionInfoAsync(
            Session session, CancellationToken cancellationToken = default)
        {
            var now = Clock.Now.ToDateTime();
            var sessionInfo = SessionInfos.GetValueOrDefault(session.Id)
                ?? new SessionInfo(session.Id) {
                    CreatedAt = now,
                    LastSeenAt = now,
                };
            return Task.FromResult(sessionInfo)!;
        }

        public virtual async Task<SessionInfo[]> GetUserSessions(
            Session session, CancellationToken cancellationToken = default)
        {
            var user = await GetUserAsync(session, cancellationToken).ConfigureAwait(false);
            if (!user.IsAuthenticated)
                return Array.Empty<SessionInfo>();

            await GetFakeUserInfo(user.Id).ConfigureAwait(false);
            var sessionIds = UserSessions.GetValueOrDefault(user.Id) ?? ImmutableHashSet<string>.Empty;
            var result = new List<SessionInfo>();
            foreach (var sessionId in sessionIds) {
                var tmpSession = new Session(sessionId);
                var sessionInfo = await GetSessionInfoAsync(tmpSession, cancellationToken).ConfigureAwait(false);
                result.Add(sessionInfo);
            }
            return result.OrderByDescending(si => si.LastSeenAt).ToArray();
        }

        [ComputeMethod]
        protected virtual Task<Unit> GetFakeUserInfo(string userId) => TaskEx.UnitTask;
    }
}
