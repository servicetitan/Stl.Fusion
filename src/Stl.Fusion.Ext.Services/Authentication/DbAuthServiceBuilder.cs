using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Authentication.Services;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Fusion.EntityFramework.Operations;

namespace Stl.Fusion.Authentication;

[StructLayout(LayoutKind.Auto)]
public readonly struct DbAuthServiceBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>
    where TDbContext : DbContext
    where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
    where TDbUser : DbUser<TDbUserId>, new()
    where TDbUserId : notnull
{
    public FusionBuilder Fusion { get; }
    public DbContextBuilder<TDbContext> DbContext { get; }
    public IServiceCollection Services => Fusion.Services;

    internal DbAuthServiceBuilder(
        FusionBuilder fusion,
        Action<DbAuthServiceBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>>? configure)
    {
        Fusion = fusion;
        DbContext = fusion.Services.AddDbContextServices<TDbContext>();
        var services = Services;
        if (services.HasService<DbSessionInfoTrimmer<TDbContext>>()) {
            configure?.Invoke(this);
            return;
        }

        DbContext.AddOperations();

        // DbAuthService
        fusion.AddAuthService<DbAuthService<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>>();
        services.TryAddSingleton<DbAuthService<TDbContext>.Options>();

        // Repositories, entity resolvers & converters, isolation level selectors
        services.TryAddSingleton<
            IDbUserRepo<TDbContext, TDbUser, TDbUserId>,
            DbUserRepo<TDbContext, TDbUser, TDbUserId>>();
        services.TryAddSingleton<
            IDbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId>,
            DbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId>>();

        // Entity converters
        DbContext.TryAddEntityConverter<TDbUser, User, DbUserConverter<TDbContext, TDbUser, TDbUserId>>();
        DbContext.TryAddEntityConverter<TDbSessionInfo, SessionInfo, DbSessionInfoConverter<TDbContext, TDbSessionInfo, TDbUserId>>();

        // Entity resolvers
        DbContext.TryAddEntityResolver<string, TDbSessionInfo>();
        DbContext.TryAddEntityResolver<TDbUserId, TDbUser>(
            _ => new DbEntityResolver<TDbContext, TDbUserId, TDbUser>.Options() {
                QueryTransformer = query => query.Include(u => u.Identities),
            });

        // DbUserIdHandler
        services.TryAddSingleton<IDbUserIdHandler<TDbUserId>, DbUserIdHandler<TDbUserId>>();

        // Default isolation level selector
        DbContext.AddOperations().TryAddIsolationLevelSelector(_ => new DbAuthIsolationLevelSelector<TDbContext>());

        // DbSessionInfoTrimmer - hosted service!
        services.TryAddSingleton<DbSessionInfoTrimmer<TDbContext>.Options>();
        services.TryAddSingleton<
            DbSessionInfoTrimmer<TDbContext>,
            DbSessionInfoTrimmer<TDbContext, TDbSessionInfo, TDbUserId>>();
        services.AddHostedService(c => c.GetRequiredService<DbSessionInfoTrimmer<TDbContext>>());

        configure?.Invoke(this);
    }

    // Core settings

    public DbAuthServiceBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId> ConfigureAuthService(
        Func<IServiceProvider, DbAuthService<TDbContext>.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }

    public DbAuthServiceBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId> ConfigureSessionInfoTrimmer(
        Func<IServiceProvider, DbSessionInfoTrimmer<TDbContext>.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }

    // Entity resolvers

    public DbAuthServiceBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId> ConfigureUserEntityResolver(
        Func<IServiceProvider, DbEntityResolver<TDbContext, TDbUserId, TDbUser>.Options> optionsFactory,
        bool includeIdentities = true)
    {
        if (!includeIdentities)
            Services.AddSingleton(optionsFactory);
        else
            Services.AddSingleton(c => {
                var options = optionsFactory.Invoke(c);
                var queryTransformer = options.QueryTransformer;
                return options with {
                    QueryTransformer = query => queryTransformer.Invoke(query).Include(u => u.Identities),
                };
            });
        return this;
    }

    public DbAuthServiceBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId> ConfigureSessionInfoEntityResolver(
        Func<IServiceProvider, DbEntityResolver<TDbContext, string, TDbSessionInfo>.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }

}
