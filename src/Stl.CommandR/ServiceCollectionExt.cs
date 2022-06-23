namespace Stl.CommandR;

public static class ServiceCollectionExt
{
    public static CommanderBuilder AddCommander(
        this IServiceCollection services,
        CommanderOptions? options = null)
        => new(services, options);

    public static IServiceCollection AddCommander(
        this IServiceCollection services,
        Action<CommanderBuilder> configureCommander)
        => services.AddCommander(options: null, configureCommander);

    public static IServiceCollection AddCommander(
        this IServiceCollection services,
        CommanderOptions? options,
        Action<CommanderBuilder> configureCommander)
    {
        var commandR = services.AddCommander(options);
        configureCommander(commandR);
        return services;
    }
}
