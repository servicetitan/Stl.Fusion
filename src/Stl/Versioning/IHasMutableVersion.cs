namespace Stl.Versioning
{
    public interface IHasMutableVersion<TVersion> : IHasVersion<TVersion>
        where TVersion : notnull
    {
        new TVersion Version { get; set; }
    }
}
