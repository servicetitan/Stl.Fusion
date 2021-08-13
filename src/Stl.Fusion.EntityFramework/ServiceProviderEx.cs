using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion.EntityFramework
{
    public static class ServiceProviderEx
    {
        public static IDbEntityResolver<TKey, TDbEntity> DbEntityResolver<TKey, TDbEntity>(this IServiceProvider services)
            where TKey : notnull
            where TDbEntity : class
            => services.GetRequiredService<IDbEntityResolver<TKey, TDbEntity>>();

        public static IDbEntityConverter<TDbEntity, TEntity> DbEntityConverter<TDbEntity, TEntity>(this IServiceProvider services)
            where TEntity : notnull
            where TDbEntity : class
            => services.GetRequiredService<IDbEntityConverter<TDbEntity, TEntity>>();
    }
}
