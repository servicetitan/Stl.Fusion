using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Stl.Fusion.Authentication;
using Stl.Serialization;

namespace Stl.Fusion.EntityFramework.Authentication
{
    [Table("Users")]
    [Index(nameof(Name))]
    public class DbUser : IHasId<long>
    {
        private readonly JsonSerialized<ImmutableDictionary<string, string>?> _claims =
            new(ImmutableDictionary<string, string>.Empty);

        [Key] public long Id { get; set; }
        public string Name { get; set; } = "";

        public string ClaimsJson {
            get => _claims.SerializedValue;
            set => _claims.SerializedValue = value;
        }

        [NotMapped, JsonIgnore]
        public ImmutableDictionary<string, string> Claims {
            get => _claims.Value ?? ImmutableDictionary<string, string>.Empty;
            set => _claims.Value = value;
        }

        public List<DbUserIdentity> Identities { get; } = new();

        public virtual User ToModel()
        {
            var user = new User(Id.ToString(), Name) {
                Claims = Claims,
                Identities = Identities.ToImmutableDictionary(
                    ui => new UserIdentity(ui.Id),
                    ui => ui.Secret)
            };
            return user;
        }

        public virtual void FromModel(User source)
        {
            if (Id.ToString() != source.Id)
                throw new ArgumentOutOfRangeException(nameof(source));

            // Updating user properties
            Claims = source.Claims.SetItems(Claims);

            // Adding new identities
            var identities = Identities.ToDictionary(ui => ui.Id);
            foreach (var (userIdentity, secret) in source.Identities) {
                if (!userIdentity.IsValid)
                    continue;
                var foundIdentity = identities.GetValueOrDefault(userIdentity.Id);
                if (foundIdentity != null) {
                    foundIdentity.Secret = secret;
                    continue;
                }
                Identities.Add(new DbUserIdentity() {
                    Id = userIdentity.Id,
                    UserId = Id,
                    Secret = secret ?? "",
                });
            }
        }
    }
}
