using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Stl.Conversion;
using Stl.Fusion.Authentication;
using Stl.Serialization;

namespace Stl.Fusion.EntityFramework.Authentication
{
    [Table("Users")]
    [Index(nameof(Name))]
    public class DbUser<TDbUserId> : IHasId<TDbUserId>
        where TDbUserId : notnull
    {
        private readonly NewtonsoftJsonSerialized<ImmutableDictionary<string, string>?> _claims =
            new(ImmutableDictionary<string, string>.Empty);

        [Key] public TDbUserId Id { get; set; } = default!;
        [MinLength(3)]
        public string Name { get; set; } = "";

        public string ClaimsJson {
            get => _claims.Data;
            set => _claims.Data = value;
        }

        [NotMapped, JsonIgnore]
        public ImmutableDictionary<string, string> Claims {
            get => _claims.Value ?? ImmutableDictionary<string, string>.Empty;
            set => _claims.Value = value;
        }

        public List<DbUserIdentity<TDbUserId>> Identities { get; } = new();

        public virtual User ToModel(IServiceProvider services)
        {
            var dbUserIdHandler = services.GetRequiredService<IDbUserIdHandler<TDbUserId>>();
            var user = new User(dbUserIdHandler.Format(Id), Name) {
                Claims = Claims,
                Identities = Identities.ToImmutableDictionary(
                    ui => new UserIdentity(ui.Id),
                    ui => ui.Secret)
            };
            return user;
        }

        public virtual void UpdateFrom(IServiceProvider services, User source)
        {
            var dbUserIdHandler = services.GetRequiredService<IDbUserIdHandler<TDbUserId>>();
            if (dbUserIdHandler.Format(Id) != source.Id)
                throw new ArgumentOutOfRangeException(nameof(source));

            // Adding new Claims
            Claims = source.Claims.SetItems(Claims);

            // Adding / updating identities
            var identities = Identities.ToDictionary(ui => ui.Id);
            foreach (var (userIdentity, secret) in source.Identities) {
                if (!userIdentity.IsValid)
                    continue;
                var foundIdentity = identities.GetValueOrDefault(userIdentity.Id);
                if (foundIdentity != null) {
                    foundIdentity.Secret = secret;
                    continue;
                }
                Identities.Add(new DbUserIdentity<TDbUserId>() {
                    Id = userIdentity.Id,
                    DbUserId = Id,
                    Secret = secret ?? "",
                });
            }
        }
    }
}
