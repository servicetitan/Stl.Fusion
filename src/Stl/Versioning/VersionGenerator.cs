namespace Stl.Versioning
{
    public abstract class VersionGenerator<TVersion>
        where TVersion : notnull
    {
        public abstract TVersion NextVersion(TVersion currentVersion = default!);
    }
}
