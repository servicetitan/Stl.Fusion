namespace Stl.Fusion.Authentication;

public record SessionAuthInfo : IHasId<Symbol>
{
    public Symbol Id { get; init; } = Symbol.Empty;

    // Authentication
    public UserIdentity AuthenticatedIdentity { get; init; }
    public string UserId { get; init; } = "";
    public bool IsSignOutForced { get; init; }
    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

    public SessionAuthInfo() { }
    public SessionAuthInfo(Symbol id) => Id = id;
}
