namespace Stl.DependencyInjection;

public record MixedModeService<T>(T Service, IServiceProvider Provider)
    where T : class
{
    public record Singleton(T Service, IServiceProvider Services) : MixedModeService<T>(Service, Services);
    public record Scoped(T Service, IServiceProvider Services) : MixedModeService<T>(Service, Services);
}
