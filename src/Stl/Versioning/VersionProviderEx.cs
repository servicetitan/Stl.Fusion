namespace Stl.Versioning
{
    public static class VersionProviderEx
    {
        public static TVersion FirstVersion<TVersion>(this IVersionProvider<TVersion> versionProvider)
            where TVersion : notnull
            => versionProvider.NextVersion(default!);

        public static TEntity UpdateVersion<TEntity, TVersion>(
            this IVersionProvider<TVersion> versionProvider,
            TEntity entity)
            where TEntity : IHasWritableVersion<TVersion>
            where TVersion : notnull
        {
            entity.Version = versionProvider.NextVersion(entity.Version);
            return entity;
        }
    }
}
