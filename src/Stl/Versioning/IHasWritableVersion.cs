namespace Stl.Versioning
{
    public interface IHasWritableVersion<TVersion> : IHasVersion<TVersion>
        where TVersion : notnull
    {
        new TVersion Version { get; set; }
    }
}
