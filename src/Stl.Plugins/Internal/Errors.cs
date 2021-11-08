namespace Stl.Plugins.Internal;

public static class Errors
{
    public static Exception UnknownPluginImplementationType(Type pluginType)
        => new KeyNotFoundException($"Unknown plugin implementation type: '{pluginType.Name}'.");
    public static Exception PluginDisabled(Type pluginType)
        => new InvalidOperationException($"Plugin '{pluginType.Name}' is disabled.");

    public static Exception MultipleSingletonPluginImplementationsFound(
        Type requestedType, Type matchingType1, Type matchingType2)
        => new InvalidOperationException(
            $"Multiple implementations of singleton plugin of type '{requestedType.Name}' found: " +
            $"{matchingType1.Name} and {matchingType2.Name}.");

    public static Exception PluginIsAbstract(Type pluginType)
        => new InvalidOperationException($"Plugin '{pluginType}' is an abstract type.");
    public static Exception PluginIsNonPublic(Type pluginType)
        => new InvalidOperationException($"Plugin '{pluginType}' is a non-public type.");

    public static Exception PluginFinderRunFailed(Type pluginFinderType)
        => new InvalidOperationException($"'{pluginFinderType}.{nameof(IPluginFinder.Run)}()' method failed.");
}
