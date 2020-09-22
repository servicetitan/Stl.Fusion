using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using Newtonsoft.Json;

namespace Stl.Fusion.Authentication
{
    public class User : IPrincipal, IIdentity, IHasId<string>
    {
        public static string GuestName { get; set; } = "Guest";
        private static readonly Dictionary<string, string> EmptyClaims = new Dictionary<string, string>();

        private readonly Lazy<ClaimsPrincipal> _claimsPrincipalLazy;

        public string AuthenticationType { get; }
        public string Id { get; }
        public string Name { get; }
        public IReadOnlyDictionary<string, string> Claims { get; }
        [JsonIgnore]
        public bool IsAuthenticated => !string.IsNullOrEmpty(AuthenticationType);
        [JsonIgnore]
        public IIdentity Identity => this;
        [JsonIgnore]
        public ClaimsPrincipal ClaimsPrincipal => _claimsPrincipalLazy.Value;

        public User(string id) : this("", id, GuestName) { }
        [JsonConstructor]
        public User(string authenticationType,
            string id, string name = "",
            IReadOnlyDictionary<string, string>? claims = null)
        {
            _claimsPrincipalLazy = new Lazy<ClaimsPrincipal>(ToClaimsPrincipal);
            AuthenticationType = authenticationType;
            Id = id;
            Name = name;
            Claims = claims ?? EmptyClaims;
        }

        public virtual bool IsInRole(string role)
            => throw new NotSupportedException();

        protected virtual ClaimsPrincipal ToClaimsPrincipal()
        {
            var claims = new List<Claim>() {
                new Claim(ClaimTypes.NameIdentifier, Id, ClaimValueTypes.String),
                new Claim(ClaimTypes.Name, Name, ClaimValueTypes.String),
            };
            foreach (var (type, value) in Claims)
                claims.Add(new Claim(type, value));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType));
        }
    }
}
