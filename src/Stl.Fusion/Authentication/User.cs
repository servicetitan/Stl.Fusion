using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Principal;
using Newtonsoft.Json;

namespace Stl.Fusion.Authentication
{
    public record User : IPrincipal, IIdentity, IHasId<string>
    {
        public static string GuestName { get; set; } = "Guest";

        private readonly Lazy<ClaimsPrincipal> _claimsPrincipalLazy;

        public string AuthenticationType { get; init; }
        public string Id { get; init; }
        public string Name { get; init; }
        public ImmutableDictionary<string, string> Claims { get; init; }
        [JsonIgnore]
        public bool IsAuthenticated => !string.IsNullOrEmpty(AuthenticationType);
        [JsonIgnore]
        public ClaimsPrincipal ClaimsPrincipal => _claimsPrincipalLazy.Value;
        IIdentity IPrincipal.Identity => this;

        // Guest user constructor
        public User(string idSuffix) : this("", $"{GuestName}:{idSuffix}", GuestName) { }

        // Primary constructor
        [JsonConstructor]
        public User(string authenticationType,
            string id, string name = "",
            ImmutableDictionary<string, string>? claims = null)
        {
            _claimsPrincipalLazy = new(ToClaimsPrincipal);
            AuthenticationType = authenticationType;
            Id = id;
            Name = name;
            Claims = claims ?? ImmutableDictionary<string, string>.Empty;
        }

        public virtual bool IsInRole(string role)
            => throw new NotSupportedException();

        protected virtual ClaimsPrincipal ToClaimsPrincipal()
        {
            var claims = new List<Claim>() {
                new(ClaimTypes.NameIdentifier, Id, ClaimValueTypes.String),
                new(ClaimTypes.Name, Name, ClaimValueTypes.String),
            };
            foreach (var (type, value) in Claims)
                claims.Add(new Claim(type, value));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType));
        }

        public virtual bool Equals(User? other)
            => ReferenceEquals(this, other);
        public override int GetHashCode()
            => RuntimeHelpers.GetHashCode(this);
    }
}
