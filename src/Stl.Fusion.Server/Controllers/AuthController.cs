using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.Server.Controllers;

[Route("fusion/auth/[action]")]
[ApiController, JsonifyErrors(RewriteErrors = true), UseDefaultSession]
public class AuthController : ControllerBase, IAuth
{
    private IAuth Auth { get; }
    private ICommander Commander { get; }

    public AuthController(IAuth auth, ICommander commander)
    {
        Auth = auth;
        Commander = commander;
    }

    // Commands

    [HttpPost]
    public Task SignOut([FromBody] SignOutCommand command, CancellationToken cancellationToken = default)
        => Commander.Call(command, cancellationToken);

    [HttpPost]
    public Task EditUser(EditUserCommand command, CancellationToken cancellationToken = default)
        => Commander.Call(command, cancellationToken);

    [HttpPost]
    public Task UpdatePresence([FromBody] Session session, CancellationToken cancellationToken = default)
        => Auth.UpdatePresence(session, cancellationToken);

    // Queries

    [HttpGet, Publish]
    public Task<bool> IsSignOutForced(Session session, CancellationToken cancellationToken = default)
        => Auth.IsSignOutForced(session, cancellationToken);

    [HttpGet, Publish]
    public Task<SessionAuthInfo?> GetAuthInfo(Session session, CancellationToken cancellationToken = default)
        => Auth.GetAuthInfo(session, cancellationToken);

    [HttpGet, Publish]
    public Task<SessionInfo?> GetSessionInfo(Session session, CancellationToken cancellationToken = default)
        => Auth.GetSessionInfo(session, cancellationToken);

    [HttpGet, Publish]
    public Task<ImmutableOptionSet> GetOptions(Session session, CancellationToken cancellationToken = default)
        => Auth.GetOptions(session, cancellationToken);

    [HttpGet, Publish]
    public Task<User?> GetUser(Session session, CancellationToken cancellationToken = default)
        => Auth.GetUser(session, cancellationToken);

    [HttpGet, Publish]
    public Task<ImmutableArray<SessionInfo>> GetUserSessions(Session session, CancellationToken cancellationToken = default)
        => Auth.GetUserSessions(session, cancellationToken);
}
