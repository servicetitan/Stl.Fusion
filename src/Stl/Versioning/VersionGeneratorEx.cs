namespace Stl.Versioning
{
    public static class VersionGeneratorEx
    {
        public static TEntity UpdateVersion<TEntity, TVersion>(
            this VersionGenerator<TVersion> versionGenerator,
            TEntity entity)
            where TEntity : IHasWritableVersion<TVersion>
            where TVersion : notnull
        {
            entity.Version = versionGenerator.NextVersion(entity.Version);
            return entity;
        }
    }
}
