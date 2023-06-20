using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Authentication.Services;

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
    public override SessionInfo NewModel() => new(Clocks.SystemClock.Now);

    public override void UpdateEntity(SessionInfo source, TDbSessionInfo target)
    {
        var session = new Session(target.Id);
        if (!Equals(session.Hash, source.SessionHash))
            throw new ArgumentOutOfRangeException(nameof(source));
        if (target.IsSignOutForced)
            throw Errors.SessionUnavailable();

        target.Version = VersionGenerator.NextVersion(target.Version);
        target.LastSeenAt = source.LastSeenAt;
        target.IPAddress = source.IPAddress;
        target.UserAgent = source.UserAgent;
        target.Options = source.Options;

        target.AuthenticatedIdentity = source.AuthenticatedIdentity;
        target.UserId = DbUserIdHandler.Parse(source.UserId, true);
        if (DbUserIdHandler.IsNone(target.UserId))
            target.UserId = default; // Should be null instead of None
        target.IsSignOutForced = source.IsSignOutForced;
    }

    public override SessionInfo UpdateModel(TDbSessionInfo source, SessionInfo target)
    {
        var session = new Session(source.Id);
        var result = source.IsSignOutForced
            ? new (session, Clocks.SystemClock.Now) {
                SessionHash = session.Hash,
                IsSignOutForced = true,
            }
            : target with {
                SessionHash = session.Hash,
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
