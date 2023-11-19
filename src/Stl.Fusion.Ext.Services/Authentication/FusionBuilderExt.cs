using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Stl.Fusion.Authentication.Services;
using Stl.Internal;
using UnreferencedCode = Stl.Fusion.Internal.UnreferencedCode;

namespace Stl.Fusion.Authentication;

public static class FusionBuilderExt
{
    // InMemoryAuthService

    public static FusionBuilder AddInMemoryAuthService(this FusionBuilder fusion)
        => fusion.AddAuthService(typeof(InMemoryAuthService));

    // DbAuthService<...>

    [RequiresUnreferencedCode(UnreferencedCode.Fusion)]
    public static FusionBuilder AddDbAuthService<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbContext,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbUserId>(
        this FusionBuilder fusion,
        Action<DbAuthServiceBuilder<TDbContext, DbSessionInfo<TDbUserId>, DbUser<TDbUserId>, TDbUserId>>? configure = null)
        where TDbContext : DbContext
        where TDbUserId : notnull
        => fusion.AddDbAuthService<TDbContext, DbSessionInfo<TDbUserId>, DbUser<TDbUserId>, TDbUserId>(configure);

    [RequiresUnreferencedCode(UnreferencedCode.Fusion)]
    public static FusionBuilder AddDbAuthService<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbContext,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbSessionInfo,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbUser,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbUserId>(
        this FusionBuilder fusion,
        Action<DbAuthServiceBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>>? configure = null)
        where TDbContext : DbContext
        where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
        where TDbUser : DbUser<TDbUserId>, new()
        where TDbUserId : notnull
        => new DbAuthServiceBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>(fusion, configure).Fusion;

    // Custom auth service

    public static FusionBuilder AddAuthService<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TAuthService>
        (this FusionBuilder fusion)
        where TAuthService : class, IAuthBackend
        => fusion.AddAuthService(typeof(TAuthService));

    public static FusionBuilder AddAuthService(
        this FusionBuilder fusion,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type implementationType)
    {
        var services = fusion.Services;
        if (services.HasService<IAuthBackend>())
            return fusion;

        var tAuthBackend = typeof(IAuthBackend);
        if (!tAuthBackend.IsAssignableFrom(implementationType))
            throw Errors.MustImplement(implementationType, tAuthBackend, nameof(implementationType));

        fusion.AddService(typeof(IAuth), implementationType, addCommandHandlers: false);
        services.AddSingleton(c => (IAuthBackend)c.GetRequiredService<IAuth>());
        fusion.Commander.AddHandlers(typeof(IAuth));
        fusion.Commander.AddHandlers(typeof(IAuthBackend));
        return fusion;
    }
}
