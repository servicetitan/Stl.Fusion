namespace Stl.Fusion.Authentication;

public record SessionAuthInfo
{
    public string SessionHash { get; init; } = "";

    // Authentication
    public UserIdentity AuthenticatedIdentity { get; init; }
    public string UserId { get; init; } = "";
    public bool IsSignOutForced { get; init; }

    public SessionAuthInfo() { }
    public SessionAuthInfo(Session? session)
        => SessionHash = session?.Hash ?? "";
}
