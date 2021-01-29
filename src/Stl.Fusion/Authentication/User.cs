using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Principal;
using Newtonsoft.Json;
using Stl.Text;

namespace Stl.Fusion.Authentication
{

    public record User : IPrincipal, IIdentity, IHasId<Symbol>
    {
        public static string GuestIdPrefix { get; } = "@guest/";
        public static string GuestName { get; } = "Guest";

        private readonly Lazy<ClaimsPrincipal> _claimsPrincipalLazy;

        public Symbol Id { get; init; }
        public string Name { get; init; }
        public ImmutableDictionary<string, string> Claims { get; init; }
        public ImmutableDictionary<UserIdentity, string> Identities { get; init; }
        [JsonIgnore]
        public bool IsAuthenticated => !(Id.IsEmpty || Id.Value.StartsWith(GuestIdPrefix));
        [JsonIgnore]
        public string AuthenticationType => IsAuthenticated ? UserIdentity.DefaultAuthenticationType : "";
        [JsonIgnore]
        public ClaimsPrincipal ClaimsPrincipal => _claimsPrincipalLazy.Value;

        // Explicit interface implementations
        IIdentity IPrincipal.Identity => this;

        // Guest user constructor
        public User(string guestIdSuffix) : this(GuestIdPrefix + guestIdSuffix, GuestName) { }
        // Primary constructor
        [JsonConstructor]
        public User(Symbol id, string name)
        {
            Id = id;
            Name = name;
            Claims = ImmutableDictionary<string, string>.Empty;
            Identities = ImmutableDictionary<UserIdentity, string>.Empty;
            _claimsPrincipalLazy = new(ToClaimsPrincipal);
        }
        // Record copy constructor.
        // Overriden to ensure _claimsPrincipalLazy is recreated.
        protected User(User other)
        {
            Id = other.Id;
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
            => Identities.IsEmpty
                ? this
                : this with {
                    Identities = ImmutableDictionary<UserIdentity, string>.Empty
                };

        // Equality is changed back to reference-based

        public virtual bool Equals(User? other) => ReferenceEquals(this, other);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

        // Protected methods

        protected virtual ClaimsPrincipal ToClaimsPrincipal()
        {
            var claims = new List<Claim>();
            if (!Id.IsEmpty)
                claims.Add(new(ClaimTypes.NameIdentifier, Id, ClaimValueTypes.String));
            if (!string.IsNullOrEmpty(Name))
                claims.Add(new(ClaimTypes.Name, Name, ClaimValueTypes.String));
            foreach (var (key, value) in Claims)
                claims.Add(new Claim(key, value));
            var claimsIdentity = new ClaimsIdentity(claims, AuthenticationType);
            return new ClaimsPrincipal(claimsIdentity);
        }
    }
}
