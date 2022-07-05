using RestEase;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.Client.Internal;

[BasePath("fusion/auth")]
public interface IAuthClientDef
{
    [Post(nameof(SignOut))]
    Task SignOut([Body] SignOutCommand command, CancellationToken cancellationToken = default);
    [Post(nameof(EditUser))]
    Task EditUser([Body] EditUserCommand command, CancellationToken cancellationToken = default);
    [Post(nameof(UpdatePresence))]
    Task UpdatePresence([Body] Session session, CancellationToken cancellationToken = default);

    [Get(nameof(IsSignOutForced))]
    Task<bool> IsSignOutForced(Session session, CancellationToken cancellationToken = default);
    [Get(nameof(GetAuthInfo))]
    Task<SessionAuthInfo?> GetAuthInfo(Session session, CancellationToken cancellationToken = default);
    [Get(nameof(GetSessionInfo))]
    Task<SessionInfo?> GetSessionInfo(Session session, CancellationToken cancellationToken = default);
    [Get(nameof(GetOptions))]
    Task<ImmutableOptionSet> GetOptions(Session session, CancellationToken cancellationToken = default);

    [Get(nameof(GetUser))]
    Task<User?> GetUser(Session session, CancellationToken cancellationToken = default);
    [Get(nameof(GetUserSessions))]
    Task<ImmutableArray<SessionInfo>> GetUserSessions(Session session, CancellationToken cancellationToken = default);
}
