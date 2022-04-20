using Microsoft.EntityFrameworkCore;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.EntityFramework.Authentication;

public class DbSessionInfoConverter<TDbContext, TDbSessionInfo, TDbUserId> 
    : DbEntityConverter<TDbContext, TDbSessionInfo, SessionInfo>
    where TDbContext : DbContext
    where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
    where TDbUserId : notnull
{
    protected IDbUserIdHandler<TDbUserId> DbUserIdHandler { get; init; }

    public DbSessionInfoConverter(IServiceProvider services) : base(services)
        => DbUserIdHandler = services.GetRequiredService<IDbUserIdHandler<TDbUserId>>();

    public override TDbSessionInfo NewEntity() => new();
    public override SessionInfo NewModel() => new(Symbol.Empty, Clocks.SystemClock.Now);

    public override void UpdateEntity(SessionInfo source, TDbSessionInfo target)
    {
        if (target.Id != source.Id)
            throw new ArgumentOutOfRangeException(nameof(source));
        if (target.IsSignOutForced)
            throw Errors.ForcedSignOut();

        target.Version = VersionGenerator.NextVersion(target.Version);
        target.LastSeenAt = source.LastSeenAt;
        target.IPAddress = source.IPAddress;
        target.UserAgent = source.UserAgent;
        target.Options = source.Options;

        target.AuthenticatedIdentity = source.AuthenticatedIdentity;
        target.UserId = DbUserIdHandler.Parse(source.UserId);
        target.IsSignOutForced = source.IsSignOutForced;
    }

    public override SessionInfo UpdateModel(TDbSessionInfo source, SessionInfo target)
    {
        var result = source.IsSignOutForced
            ? new (source.Id, Clocks.SystemClock.Now) {
                IsSignOutForced = true,
            }
            : target with {
                Id = source.Id,
                Version = source.Version,
                CreatedAt = source.CreatedAt,
                LastSeenAt = source.LastSeenAt,
                IPAddress = source.IPAddress,
                UserAgent = source.UserAgent,
                Options = source.Options,

                // Authentication
                AuthenticatedIdentity = source.AuthenticatedIdentity,
                UserId = DbUserIdHandler.Format(source.UserId),
                IsSignOutForced = source.IsSignOutForced,
            };
        return result;
    }
}
