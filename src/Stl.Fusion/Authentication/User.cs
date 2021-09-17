using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Json.Serialization;
using Stl.Text;
using Stl.Versioning;

namespace Stl.Fusion.Authentication
{
    [DataContract]
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
    public record User : IPrincipal, IIdentity, IHasId<Symbol>, IHasVersion<long>
    {
        public static string GuestIdPrefix { get; } = "@guest/";
        public static string GuestName { get; } = "Guest";

        private readonly Lazy<ClaimsPrincipal> _claimsPrincipalLazy;

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
        [JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public bool IsAuthenticated => !(Id.IsEmpty || Id.Value.StartsWith(GuestIdPrefix));
        [JsonIgnore, Newtonsoft.Json.JsonIgnore]
        string IIdentity.AuthenticationType => IsAuthenticated ? UserIdentity.DefaultSchema : "";
        [JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public ClaimsPrincipal ClaimsPrincipal => _claimsPrincipalLazy.Value;

        [DataMember(Name = nameof(Identities))]
        [JsonPropertyName(nameof(Identities)),  Newtonsoft.Json.JsonProperty(nameof(Identities))]
        public Dictionary<string, string> JsonCompatibleIdentities {
            get => Identities.ToDictionary(p => p.Key.Id.Value, p => p.Value);
            init => Identities = value.ToImmutableDictionary(p => new UserIdentity(p.Key), p => p.Value);
        }

        // Explicit interface implementations
        IIdentity IPrincipal.Identity => this;

        // Guest user constructor
        public User(string guestIdSuffix) : this(GuestIdPrefix + guestIdSuffix, GuestName) { }
        // Primary constructor
        public User(Symbol id, string name)
        {
            Id = id;
            Name = name;
            Claims = ImmutableDictionary<string, string>.Empty;
            Identities = ImmutableDictionary<UserIdentity, string>.Empty;
            _claimsPrincipalLazy = new(ToClaimsPrincipal);
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
            JsonCompatibleIdentities = jsonCompatibleIdentities;
            _claimsPrincipalLazy = new(ToClaimsPrincipal);
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
            _claimsPrincipalLazy = new(ToClaimsPrincipal);
        }

        public User WithClaim(string name, string value)
            => this with { Claims = Claims.SetItem(name, value) };
        public User WithIdentity(UserIdentity identity, string secret = "")
            => this with { Identities = Identities.SetItem(identity, secret) };

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

        // Equality is changed back to reference-based

        public virtual bool Equals(User? other) => ReferenceEquals(this, other);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

        // Protected methods

        protected virtual ClaimsPrincipal ToClaimsPrincipal()
        {
            var claims = new List<Claim>();
            if (!Id.IsEmpty)
                claims.Add(new(ClaimTypes.NameIdentifier, Id, ClaimValueTypes.String));
            claims.Add(new(ClaimTypes.Version, Version.ToString(), ClaimValueTypes.String));
            if (!string.IsNullOrEmpty(Name))
                claims.Add(new(ClaimTypes.Name, Name, ClaimValueTypes.String));
            foreach (var (key, value) in Claims)
                claims.Add(new Claim(key, value));
            var identity = (IIdentity) this;
            var claimsIdentity = new ClaimsIdentity(claims, identity.AuthenticationType);
            return new ClaimsPrincipal(claimsIdentity);
        }
    }
}
