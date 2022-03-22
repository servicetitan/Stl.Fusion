using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.Server.Controllers;

[Route("fusion/auth/[action]")]
[ApiController, JsonifyErrors(RewriteErrors = true), UseDefaultSession]
public class AuthController : ControllerBase, IAuth
{
    protected IAuth Auth { get; }

    public AuthController(IAuth auth) 
        => Auth = auth;

    // Commands

    [HttpPost]
    public Task SignOut([FromBody] SignOutCommand command, CancellationToken cancellationToken = default)
        => Auth.SignOut(command, cancellationToken);

    [HttpPost]
    public Task EditUser(EditUserCommand command, CancellationToken cancellationToken = default)
        => Auth.EditUser(command, cancellationToken);

    [HttpPost]
    public Task UpdatePresence([FromBody] Session session, CancellationToken cancellationToken = default)
        => Auth.UpdatePresence(session, cancellationToken);

    // Queries

    [HttpGet, Publish]
    public Task<bool> IsSignOutForced(Session session, CancellationToken cancellationToken = default)
        => Auth.IsSignOutForced(session, cancellationToken);

    [HttpGet, Publish]
    public Task<SessionAuthInfo> GetAuthInfo(Session session, CancellationToken cancellationToken = default)
        => Auth.GetAuthInfo(session, cancellationToken);

    [HttpGet, Publish]
    public Task<SessionInfo?> GetSessionInfo(Session session, CancellationToken cancellationToken = default)
        => Auth.GetSessionInfo(session, cancellationToken);

    [HttpGet, Publish]
    public Task<ImmutableOptionSet> GetOptions(Session session, CancellationToken cancellationToken = default)
        => Auth.GetOptions(session, cancellationToken);

    [HttpGet, Publish]
    public Task<User> GetUser(Session session, CancellationToken cancellationToken = default)
        => Auth.GetUser(session, cancellationToken);

    [HttpGet, Publish]
    public Task<SessionInfo[]> GetUserSessions(Session session, CancellationToken cancellationToken = default)
        => Auth.GetUserSessions(session, cancellationToken);
}
