namespace Stl.Plugins;

public interface IHasDependencies
{
    IEnumerable<Type> Dependencies { get; }
}
