using Microsoft.EntityFrameworkCore;
using Stl.Fusion.Authentication.Services;
using Stl.Internal;

namespace Stl.Fusion.Authentication;

public static class FusionBuilderExt
{
    // InMemoryAuthService

    public static FusionBuilder AddInMemoryAuthService(this FusionBuilder fusion, bool expose = true)
        => fusion.AddAuthService(typeof(InMemoryAuthService), expose);

    // DbAuthService<...>

    public static DbAuthServiceBuilder<TDbContext, DbSessionInfo<TDbUserId>, DbUser<TDbUserId>, TDbUserId>
        AddDbAuthService<TDbContext, TDbUserId>(this FusionBuilder fusion, bool expose = true)
        where TDbContext : DbContext
        where TDbUserId : notnull
        => fusion.AddDbAuthService<TDbContext, DbSessionInfo<TDbUserId>, DbUser<TDbUserId>, TDbUserId>(expose);

    public static DbAuthServiceBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>
        AddDbAuthService<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>(
            this FusionBuilder fusion, bool expose = true)
        where TDbContext : DbContext
        where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
        where TDbUser : DbUser<TDbUserId>, new()
        where TDbUserId : notnull
        => new(fusion, null, expose);

    public static FusionBuilder AddDbAuthService<TDbContext, TDbUserId>(
        this FusionBuilder fusion,
        Action<DbAuthServiceBuilder<TDbContext, DbSessionInfo<TDbUserId>, DbUser<TDbUserId>, TDbUserId>> configure,
        bool expose = true)
        where TDbContext : DbContext
        where TDbUserId : notnull
        => fusion.AddDbAuthService<TDbContext, DbSessionInfo<TDbUserId>, DbUser<TDbUserId>, TDbUserId>(configure, expose);

    public static FusionBuilder AddDbAuthService<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>(
        this FusionBuilder fusion,
        Action<DbAuthServiceBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>> configure,
        bool expose = true)
        where TDbContext : DbContext
        where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
        where TDbUser : DbUser<TDbUserId>, new()
        where TDbUserId : notnull
        => new DbAuthServiceBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>(fusion, configure, expose).Fusion;

    // Custom auth service

    public static FusionBuilder AddAuthService<TAuthService>(this FusionBuilder fusion, bool expose = true)
        where TAuthService : class, IAuthBackend
        => fusion.AddAuthService(typeof(TAuthService), expose);

    public static FusionBuilder AddAuthService(this FusionBuilder fusion, Type implementationType, bool expose = true)
    {
        var services = fusion.Services;
        if (services.Any(d => d.ServiceType == typeof(IAuthBackend)))
            return fusion;

        var serverSideServiceType = typeof(IAuthBackend);
        if (!serverSideServiceType.IsAssignableFrom(implementationType))
            throw Errors.MustImplement(implementationType, serverSideServiceType, nameof(implementationType));

        if (expose)
            fusion.AddServer(typeof(IAuth), implementationType);
        else
            fusion.AddService(typeof(IAuth), implementationType);
        services.AddSingleton(c => (IAuthBackend)c.GetRequiredService(implementationType));
        return fusion;
    }
}
