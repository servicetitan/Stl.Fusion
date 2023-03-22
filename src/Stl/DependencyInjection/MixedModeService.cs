namespace Stl.DependencyInjection;

public abstract record MixedModeService<T>(T Service, IServiceProvider Services)
    where T : class
{
    public record Singleton(T Service, IServiceProvider Services)
        : MixedModeService<T>(Service, Services);
    public record Scoped(T Service, IServiceProvider Services)
        : MixedModeService<T>(Service, Services);
}
