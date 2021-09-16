namespace Stl.Versioning
{
    public static class HasMutableVersionExt
    {
        public static TEntity UpdateVersion<TEntity, TVersion>(
            this TEntity entity,
            VersionGenerator<TVersion> versionGenerator)
            where TEntity : IHasMutableVersion<TVersion>
            where TVersion : notnull
        {
            entity.Version = versionGenerator.NextVersion(entity.Version);
            return entity;
        }
    }
}
