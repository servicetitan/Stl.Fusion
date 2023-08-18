using System.Security;
using Stl.Versioning;

namespace Stl.Fusion.Authentication;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record SessionInfo : SessionAuthInfo, IHasVersion<long>
{
    public static new Requirement<SessionInfo> MustBeAuthenticated { get; set; } = Requirement.New(
        new("Session is not authenticated.", m => new SecurityException(m)),
        (SessionInfo? i) => i?.IsAuthenticated() ?? false);

    [DataMember(Order = 10), MemoryPackOrder(10)] public long Version { get; init; }
    [DataMember(Order = 11), MemoryPackOrder(11)] public Moment CreatedAt { get; init; }
    [DataMember(Order = 12), MemoryPackOrder(12)] public Moment LastSeenAt { get; init; }
    [DataMember(Order = 13), MemoryPackOrder(13)] public string IPAddress { get; init; } = "";
    [DataMember(Order = 14), MemoryPackOrder(14)] public string UserAgent { get; init; } = "";
    [DataMember(Order = 15), MemoryPackOrder(15)] public ImmutableOptionSet Options { get; init; } = ImmutableOptionSet.Empty;

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public SessionInfo() { }
    public SessionInfo(Moment createdAt) : this(null, createdAt) { }
    public SessionInfo(Session? session, Moment createdAt = default) : base(session)
    {
        CreatedAt = createdAt;
        LastSeenAt = createdAt;
    }

    public SessionAuthInfo ToAuthInfo()
        => IsSignOutForced
            ? new() {
                SessionHash = SessionHash,
                IsSignOutForced = true,
            }
            : new() {
                SessionHash = SessionHash,
                AuthenticatedIdentity = AuthenticatedIdentity,
                UserId = UserId,
            };
}
