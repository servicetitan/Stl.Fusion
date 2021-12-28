using Stl.Versioning;

namespace Stl.Fusion.Authentication;

public record SessionInfo : SessionAuthInfo, IHasVersion<long>
{
    public long Version { get; init; }
    public Moment CreatedAt { get; init; }
    public Moment LastSeenAt { get; init; }
    public string IPAddress { get; init; } = "";
    public string UserAgent { get; init; } = "";
    public ImmutableOptionSet Options { get; init; } = ImmutableOptionSet.Empty;

    public SessionInfo() { }
    public SessionInfo(Symbol id, Moment createdAt = default) : base(id)
    {
        CreatedAt = createdAt;
        LastSeenAt = createdAt;
    }

    public SessionAuthInfo ToAuthInfo()
        => new (Id) {
            AuthenticatedIdentity = AuthenticatedIdentity,
            UserId = UserId,
            IsSignOutForced = IsSignOutForced,
        };
}
