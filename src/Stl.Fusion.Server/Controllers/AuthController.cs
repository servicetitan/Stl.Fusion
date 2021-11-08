using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.Server.Controllers;

[Route("fusion/auth/[action]")]
[ApiController, JsonifyErrors(RewriteErrors = true)]
public class AuthController : ControllerBase, IAuth
{
    protected IAuth Auth { get; }
    protected ISessionResolver SessionResolver { get; }

    public AuthController(IAuth auth, ISessionResolver sessionResolver)
    {
        Auth = auth;
        SessionResolver = sessionResolver;
    }

    // Commands

    [HttpPost]
    public Task SignOut([FromBody] SignOutCommand command, CancellationToken cancellationToken = default)
        => Auth.SignOut(command.UseDefaultSession(SessionResolver), cancellationToken);

    [HttpPost]
    public Task EditUser(EditUserCommand command, CancellationToken cancellationToken = default)
        => Auth.EditUser(command.UseDefaultSession(SessionResolver), cancellationToken);

    [HttpPost]
    public Task UpdatePresence([FromBody] Session session, CancellationToken cancellationToken = default)
        => Auth.UpdatePresence(session, cancellationToken);

    // Queries

    [HttpGet, Publish]
    public Task<bool> IsSignOutForced(Session session, CancellationToken cancellationToken = default)
        => Auth.IsSignOutForced(session, cancellationToken);

    [HttpGet, Publish]
    public Task<SessionInfo> GetSessionInfo(Session session, CancellationToken cancellationToken = default)
        => Auth.GetSessionInfo(session, cancellationToken);

    [HttpGet, Publish]
    public Task<User> GetSessionUser(Session session, CancellationToken cancellationToken = default)
        => Auth.GetSessionUser(session, cancellationToken);

    [HttpGet, Publish]
    public Task<SessionInfo[]> GetUserSessions(Session session, CancellationToken cancellationToken = default)
        => Auth.GetUserSessions(session, cancellationToken);

    // Non-[ComputeMethod] queries

    [HttpGet, Publish]
    public Task<Session> GetSession(CancellationToken cancellationToken = default)
        => Task.FromResult(SessionResolver.Session);
}
