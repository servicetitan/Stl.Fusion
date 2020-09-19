using System;
using System.Security.Claims;
using System.Security.Principal;
using Newtonsoft.Json;

namespace Stl.Fusion.Authentication
{
    public class Principal : IPrincipal, IIdentity, IHasId<string>
    {
        public static string GuestName { get; set; } = "Guest";

        private readonly Lazy<ClaimsPrincipal> _claimsPrincipalLazy;

        public string Id { get; }
        public string Name { get; }
        public string AuthenticationType { get; }
        [JsonIgnore]
        public bool IsAuthenticated => !string.IsNullOrEmpty(AuthenticationType);
        [JsonIgnore]
        public IIdentity Identity => this;
        [JsonIgnore]
        public ClaimsPrincipal ClaimsPrincipal => _claimsPrincipalLazy.Value;

        public Principal(string id) : this(id, GuestName, "") { }
        [JsonConstructor]
        public Principal(string id, string name, string authenticationType)
        {
            _claimsPrincipalLazy = new Lazy<ClaimsPrincipal>(ToClaimsPrincipal);
            Id = id;
            Name = name;
            AuthenticationType = authenticationType;
        }

        public virtual bool IsInRole(string role)
            => throw new NotSupportedException();

        protected virtual ClaimsPrincipal ToClaimsPrincipal()
            => new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, Id, ClaimValueTypes.String),
                new Claim(ClaimTypes.Name, Name, ClaimValueTypes.String),
            }, AuthenticationType));
    }
}
