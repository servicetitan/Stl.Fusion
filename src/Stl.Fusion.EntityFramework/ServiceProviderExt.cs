using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework;

public static class ServiceProviderExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbHub<TDbContext> DbHub<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbContext>
        (this IServiceProvider services)
        where TDbContext : DbContext
        => services.GetRequiredService<DbHub<TDbContext>>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IDbEntityResolver<TKey, TDbEntity> DbEntityResolver<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TKey,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbEntity>
        (this IServiceProvider services)
        where TKey : notnull
        where TDbEntity : class
        => services.GetRequiredService<IDbEntityResolver<TKey, TDbEntity>>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IDbEntityConverter<TDbEntity, TEntity> DbEntityConverter<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEntity>
        (this IServiceProvider services)
        where TEntity : notnull
        where TDbEntity : class
        => services.GetRequiredService<IDbEntityConverter<TDbEntity, TEntity>>();
}
