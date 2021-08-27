namespace Stl.Versioning
{
    public interface IVersionProvider<TVersion>
        where TVersion : notnull
    {
        TVersion NextVersion(TVersion currentVersion);
    }
}
