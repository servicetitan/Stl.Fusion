using Microsoft.EntityFrameworkCore;
using Stl.Fusion.Authentication.Services;
using Stl.Internal;

namespace Stl.Fusion.Authentication;

public static class FusionBuilderExt
{
    // InMemoryAuthService

    public static FusionBuilder AddInMemoryAuthService(this FusionBuilder fusion)
        => fusion.AddAuthService(typeof(InMemoryAuthService));

    // DbAuthService<...>

    public static FusionBuilder AddDbAuthService<TDbContext, TDbUserId>(
        this FusionBuilder fusion,
        Action<DbAuthServiceBuilder<TDbContext, DbSessionInfo<TDbUserId>, DbUser<TDbUserId>, TDbUserId>>? configure = null)
        where TDbContext : DbContext
        where TDbUserId : notnull
        => fusion.AddDbAuthService<TDbContext, DbSessionInfo<TDbUserId>, DbUser<TDbUserId>, TDbUserId>(configure);

    public static FusionBuilder AddDbAuthService<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>(
        this FusionBuilder fusion,
        Action<DbAuthServiceBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>>? configure = null)
        where TDbContext : DbContext
        where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
        where TDbUser : DbUser<TDbUserId>, new()
        where TDbUserId : notnull
        => new DbAuthServiceBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>(fusion, configure).Fusion;

    // Custom auth service

    public static FusionBuilder AddAuthService<TAuthService>(this FusionBuilder fusion)
        where TAuthService : class, IAuthBackend
        => fusion.AddAuthService(typeof(TAuthService));

    public static FusionBuilder AddAuthService(this FusionBuilder fusion, Type implementationType)
    {
        var services = fusion.Services;
        if (services.HasService<IAuthBackend>())
            return fusion;

        var tAuthBackend = typeof(IAuthBackend);
        if (!tAuthBackend.IsAssignableFrom(implementationType))
            throw Errors.MustImplement(implementationType, tAuthBackend, nameof(implementationType));

        fusion.AddService(typeof(IAuth), implementationType);
        services.AddSingleton(c => (IAuthBackend)c.GetRequiredService<IAuth>());
        fusion.Commander.AddHandlers<IAuthBackend>();
        return fusion;
    }
}
