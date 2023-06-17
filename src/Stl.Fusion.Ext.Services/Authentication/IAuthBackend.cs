namespace Stl.Fusion.Authentication;

public interface IAuthBackend : IComputeService
{
    // Commands
    [CommandHandler]
    Task SignIn(AuthBackend_SignIn command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task<SessionInfo> SetupSession(AuthBackend_SetupSession command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task SetOptions(Auth_SetSessionOptions command, CancellationToken cancellationToken = default);

    // Queries
    [ComputeMethod(MinCacheDuration = 10)]
    Task<User?> GetUser(Symbol tenantId, Symbol userId, CancellationToken cancellationToken = default);
}
