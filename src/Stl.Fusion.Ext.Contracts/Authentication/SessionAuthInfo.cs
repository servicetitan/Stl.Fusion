using System.Security;

namespace Stl.Fusion.Authentication;

public record SessionAuthInfo : IRequirementTarget
{
    public static Requirement<SessionAuthInfo> MustBeAuthenticated { get; set; } = Requirement.New(
        new("Session is not authenticated.", m => new SecurityException(m)),
        (SessionAuthInfo? i) => i?.IsAuthenticated() ?? false);

    public string SessionHash { get; init; } = "";

    // Authentication
    public UserIdentity AuthenticatedIdentity { get; init; }
    public Symbol UserId { get; init; } = Symbol.Empty;
    public bool IsSignOutForced { get; init; }

    public SessionAuthInfo() { }
    public SessionAuthInfo(Session? session)
        => SessionHash = session?.Hash ?? "";

    public bool IsAuthenticated()
        => !(IsSignOutForced || UserId.IsEmpty);
}
