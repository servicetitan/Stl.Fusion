using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Text;
using Stl.Versioning;

namespace Stl.Fusion.EntityFramework.Authentication
{
    public class DbUserConverter<TDbContext, TDbUser, TDbUserId> : DbEntityConverter<TDbContext, TDbUser, User>
        where TDbContext : DbContext
        where TDbUser : DbUser<TDbUserId>, new()
        where TDbUserId : notnull
    {
        protected IDbUserIdHandler<TDbUserId> DbUserIdHandler { get; init; }

        public DbUserConverter(IServiceProvider services) : base(services)
            => DbUserIdHandler = Services.GetRequiredService<IDbUserIdHandler<TDbUserId>>();

        public override TDbUser NewEntity() => new();
        public override User NewModel() => new(Symbol.Empty, "");

        public override void UpdateEntity(User source, TDbUser target)
        {
            if (DbUserIdHandler.Format(target.Id) != source.Id)
                throw new ArgumentOutOfRangeException(nameof(source));
            target.UpdateVersion(VersionGenerator);

            // Adding new Claims
            target.Claims = source.Claims.SetItems(target.Claims);

            // Adding / updating identities
            var identities = target.Identities.ToDictionary(ui => ui.Id);
            foreach (var (userIdentity, secret) in source.Identities) {
                if (!userIdentity.IsValid)
                    continue;
                var foundIdentity = identities.GetValueOrDefault(userIdentity.Id);
                if (foundIdentity != null) {
                    foundIdentity.Secret = secret;
                    continue;
                }
                target.Identities.Add(new DbUserIdentity<TDbUserId>() {
                    Id = userIdentity.Id,
                    DbUserId = target.Id,
                    Secret = secret ?? "",
                });
            }
        }

        public override User UpdateModel(TDbUser source, User target)
            => target with {
                Id = DbUserIdHandler.Format(source.Id),
                Version = source.Version,
                Name = source.Name,
                Claims = source.Claims,
                Identities = source.Identities.ToImmutableDictionary(
                    ui => new UserIdentity(ui.Id),
                    ui => ui.Secret)
            };
    }
}
