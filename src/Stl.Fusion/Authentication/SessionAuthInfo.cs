using System.Security;
using Stl.Requirements;

namespace Stl.Fusion.Authentication;

public record SessionAuthInfo : IRequirementTarget
{
    public static FuncRequirement<SessionAuthInfo> MustBeAuthenticated { get; } = Requirement.New(
        new("Session is not authenticated.", m => new SecurityException(m)),
        (SessionAuthInfo? i) => i?.IsAuthenticated() ?? false);

    public string SessionHash { get; init; } = "";

    // Authentication
    public UserIdentity AuthenticatedIdentity { get; init; }
    public string UserId { get; init; } = "";
    public bool IsSignOutForced { get; init; }

    public SessionAuthInfo() { }
    public SessionAuthInfo(Session? session)
        => SessionHash = session?.Hash ?? "";

    public bool IsAuthenticated()
        => !(IsSignOutForced || UserId.IsNullOrEmpty());
}
