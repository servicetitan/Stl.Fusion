using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Authentication;
using Stl.Fusion.EntityFramework.Authentication;
using Stl.Fusion.EntityFramework.Internal;

namespace Stl.Fusion.EntityFramework;

public readonly struct DbAuthenticationBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>
    where TDbContext : DbContext
    where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
    where TDbUser : DbUser<TDbUserId>, new()
    where TDbUserId : notnull
{
    public DbContextBuilder<TDbContext> DbContext { get; }
    public IServiceCollection Services => DbContext.Services;

    internal DbAuthenticationBuilder(
        DbContextBuilder<TDbContext> dbContext,
        Action<DbAuthenticationBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>>? configure)
    {
        DbContext = dbContext;
        if (!Services.HasService<DbOperationScope<TDbContext>>())
            throw Errors.NoOperationsFrameworkServices();
        if (Services.HasService<DbSessionInfoTrimmer<TDbContext>>()) {
            configure?.Invoke(this);
            return;
        }

        // DbAuthService
        var fusion = Services.AddFusion();
        var fusionAuth = fusion.AddAuthentication();
        Services.TryAddSingleton<DbAuthService<TDbContext>.Options>();
        fusionAuth.AddBackend<DbAuthService<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>>();

        // Repositories, entity resolvers & converters, isolation level selectors
        Services.TryAddSingleton<
            IDbUserRepo<TDbContext, TDbUser, TDbUserId>,
            DbUserRepo<TDbContext, TDbUser, TDbUserId>>();
        Services.TryAddSingleton<
            IDbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId>,
            DbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId>>();

        // Entity converters
        DbContext.AddEntityConverter<TDbUser, User, 
            DbUserConverter<TDbContext, TDbUser, TDbUserId>>();
        DbContext.AddEntityConverter<TDbSessionInfo, SessionInfo, 
            DbSessionInfoConverter<TDbContext, TDbSessionInfo, TDbUserId>>();

        // Entity resolvers
        DbContext.AddEntityResolver<string, TDbSessionInfo>();
        DbContext.AddEntityResolver<TDbUserId, TDbUser>(
            _ => new DbEntityResolver<TDbContext, TDbUserId, TDbUser>.Options() {
                QueryTransformer = query => query.Include(u => u.Identities),
            });

        // DbUserIdHandler
        Services.AddSingleton<IDbUserIdHandler<TDbUserId>, DbUserIdHandler<TDbUserId>>();

        // Default isolation level selector
        DbContext.AddOperations().AddIsolationLevelSelector(_ => new DbAuthIsolationLevelSelector<TDbContext>());

        // DbSessionInfoTrimmer - hosted service!
        Services.TryAddSingleton<DbSessionInfoTrimmer<TDbContext>.Options>();
        Services.TryAddSingleton<
            DbSessionInfoTrimmer<TDbContext>,
            DbSessionInfoTrimmer<TDbContext, TDbSessionInfo, TDbUserId>>();
        Services.AddHostedService(c => c.GetRequiredService<DbSessionInfoTrimmer<TDbContext>>());

        configure?.Invoke(this);
    }

    // Core settings

    public DbAuthenticationBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId> ConfigureAuthService(
        Func<IServiceProvider, DbAuthService<TDbContext>.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }

    public DbAuthenticationBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId> ConfigureSessionInfoTrimmer(
        Func<IServiceProvider, DbSessionInfoTrimmer<TDbContext>.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }

    // Entity resolvers

    public DbAuthenticationBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId> ConfigureUserEntityResolver(
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

    public DbAuthenticationBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId> ConfigureSessionInfoEntityResolver(
        Func<IServiceProvider, DbEntityResolver<TDbContext, string, TDbSessionInfo>.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }

}
