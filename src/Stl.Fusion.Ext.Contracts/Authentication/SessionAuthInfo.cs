using System.Security;

namespace Stl.Fusion.Authentication;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record SessionAuthInfo : IRequirementTarget
{
    public static Requirement<SessionAuthInfo> MustBeAuthenticated { get; set; } = Requirement.New(
        new("Session is not authenticated.", m => new SecurityException(m)),
        (SessionAuthInfo? i) => i?.IsAuthenticated() ?? false);

    [DataMember(Order = 0), MemoryPackOrder(0)] public string SessionHash { get; init; } = "";

    // Authentication
    [DataMember(Order = 1), MemoryPackOrder(1)] public UserIdentity AuthenticatedIdentity { get; init; }
    [DataMember(Order = 2), MemoryPackOrder(2)] public Symbol UserId { get; init; } = Symbol.Empty;
    [DataMember(Order = 3), MemoryPackOrder(3)] public bool IsSignOutForced { get; init; }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public SessionAuthInfo() { }
    public SessionAuthInfo(Session? session)
        => SessionHash = session?.Hash ?? "";

    public bool IsAuthenticated()
        => !(IsSignOutForced || UserId.IsEmpty);
}
