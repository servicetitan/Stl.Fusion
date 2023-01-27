using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework;

public static class DbEntityResolverExt
{
    public static Task<TDbEntity?> Get<TKey, TDbEntity>(
        this IDbEntityResolver<TKey, TDbEntity> resolver,
        TKey key,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        where TDbEntity : class
        => resolver.Get(Tenant.Default, key, cancellationToken);

    public static Task<Dictionary<TKey, TDbEntity>> GetMany<TKey, TDbEntity>(
        this IDbEntityResolver<TKey, TDbEntity> resolver,
        IEnumerable<TKey> keys,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        where TDbEntity : class
        => resolver.GetMany(Tenant.Default, keys, cancellationToken);

    public static async Task<Dictionary<TKey, TDbEntity>> GetMany<TKey, TDbEntity>(
        this IDbEntityResolver<TKey, TDbEntity> resolver,
        Tenant tenant,
        IEnumerable<TKey> keys,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        where TDbEntity : class
    {
        var entities = await keys
            .Distinct()
            .Select(key => resolver.Get(tenant.Id, key, cancellationToken))
            .Collect()
            .ConfigureAwait(false);
        var result = new Dictionary<TKey, TDbEntity>();
        foreach (var entity in entities)
            if (entity != null!)
                result.Add(resolver.KeyExtractor(entity), entity);
        return result;
    }
}
