using System.Globalization;
using System.Security.Claims;
using Stl.Versioning;

namespace Stl.Fusion.Authentication;

[DataContract]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public record User : IHasId<Symbol>, IHasVersion<long>
{
    public static string GuestName { get; set; } = "Guest";

    private Lazy<ClaimsPrincipal>? _claimsPrincipalLazy;

    [DataMember]
    public Symbol Id { get; init; }
    [DataMember]
    public string Name { get; init; }
    [DataMember]
    public long Version { get; init; }
    [DataMember]
    public ImmutableDictionary<string, string> Claims { get; init; }
    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public ImmutableDictionary<UserIdentity, string> Identities { get; init; }

    [DataMember(Name = nameof(Identities))]
    [JsonPropertyName(nameof(Identities)),  Newtonsoft.Json.JsonProperty(nameof(Identities))]
    public Dictionary<string, string> JsonCompatibleIdentities {
        get => Identities.ToDictionary(p => p.Key.Id.Value, p => p.Value, StringComparer.Ordinal);
        init => Identities = value.ToImmutableDictionary(p => new UserIdentity(p.Key), p => p.Value);
    }

    public static User NewGuest(string? name = null)
        => new(name ?? GuestName);

    public User(string name) : this(Symbol.Empty, name) { }
    public User(Symbol id, string name)
    {
        Id = id;
        Name = name;
        Claims = ImmutableDictionary<string, string>.Empty;
        Identities = ImmutableDictionary<UserIdentity, string>.Empty;
    }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor]
    public User(
        Symbol id,
        string name,
        long version,
        ImmutableDictionary<string, string> claims,
        Dictionary<string, string> jsonCompatibleIdentities)
    {
        Id = id;
        Name = name;
        Version = version;
        Claims = claims;
        Identities = ImmutableDictionary<UserIdentity, string>.Empty;
        JsonCompatibleIdentities = jsonCompatibleIdentities;
    }

    // Record copy constructor.
    // Overriden to ensure _claimsPrincipalLazy is recreated.
    protected User(User other)
    {
        Id = other.Id;
        Version = other.Version;
        Name = other.Name;
        Claims = other.Claims;
        Identities = other.Identities;
        _claimsPrincipalLazy = new(CreateClaimsPrincipal);
    }

    public User WithClaim(string name, string value)
        => this with { Claims = Claims.SetItem(name, value) };
    public User WithIdentity(UserIdentity identity, string secret = "")
        => this with { Identities = Identities.SetItem(identity, secret) };

    public bool IsAuthenticated()
        => !Id.IsEmpty;
    public bool IsGuest()
        => Id.IsEmpty;
    public virtual bool IsInRole(string role)
        => Claims.ContainsKey($"{ClaimTypes.Role}/{role}");

    public virtual User ToClientSideUser()
    {
        if (Identities.IsEmpty)
            return this;
        var maskedIdentities = ImmutableDictionary<UserIdentity, string>.Empty;
        foreach (var (id, _) in Identities)
            maskedIdentities = maskedIdentities.Add((id.Schema, "<hidden>"), "");
        return this with { Identities = maskedIdentities };
    }

    public ClaimsPrincipal ToClaimsPrincipal()
        => (_claimsPrincipalLazy ??= new(CreateClaimsPrincipal)).Value;

    // Equality is changed back to reference-based

    public virtual bool Equals(User? other) => ReferenceEquals(this, other);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    // Protected methods

    protected virtual ClaimsPrincipal CreateClaimsPrincipal()
    {
        var claims = new List<Claim>();
        if (IsGuest()) {
            // Guest (not authenticated)
            if (!string.IsNullOrEmpty(Name))
                claims.Add(new(ClaimTypes.Name, Name, ClaimValueTypes.String));
            foreach (var (key, value) in Claims)
                claims.Add(new Claim(key, value));
            var claimsIdentity = new ClaimsIdentity(claims);
            return new ClaimsPrincipal(claimsIdentity);
        }
        else {
            // Authenticated
            claims.Add(new Claim(ClaimTypes.NameIdentifier, Id, ClaimValueTypes.String));
            claims.Add(new(ClaimTypes.Version, Version.ToString(CultureInfo.InvariantCulture), ClaimValueTypes.String));
            if (!string.IsNullOrEmpty(Name))
                claims.Add(new(ClaimTypes.Name, Name, ClaimValueTypes.String));
            foreach (var (key, value) in Claims)
                claims.Add(new Claim(key, value));
            var claimsIdentity = new ClaimsIdentity(claims, UserIdentity.DefaultSchema);
            return new ClaimsPrincipal(claimsIdentity);
        }
    }
}
