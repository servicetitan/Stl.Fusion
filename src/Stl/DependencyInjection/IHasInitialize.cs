namespace Stl.DependencyInjection;

public interface IHasInitialize
{
    void Initialize(object? settings = null);
}
