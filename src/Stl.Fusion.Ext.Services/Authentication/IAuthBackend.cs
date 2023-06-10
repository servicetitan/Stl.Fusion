using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.Authentication;

public interface IAuthBackend : IComputeService
{
    // Commands
    [CommandHandler]
    Task SignIn(SignInCommand command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task<SessionInfo> SetupSession(SetupSessionCommand command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task SetOptions(SetSessionOptionsCommand command, CancellationToken cancellationToken = default);

    // Queries
    [ComputeMethod(MinCacheDuration = 10)]
    Task<User?> GetUser(Symbol tenantId, Symbol userId, CancellationToken cancellationToken = default);
}
