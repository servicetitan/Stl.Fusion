namespace Stl.Plugins.Internal;

public static class Errors
{
    public static Exception UnknownPluginImplementationType(Type pluginType)
        => new KeyNotFoundException($"Unknown plugin implementation type: '{pluginType.GetName()}'.");
    public static Exception PluginDisabled(Type pluginType)
        => new InvalidOperationException($"Plugin '{pluginType.GetName()}' is disabled.");

    public static Exception MultipleSingletonPluginImplementationsFound(
        Type requestedType, Type matchingType1, Type matchingType2)
        => new InvalidOperationException(
            $"Multiple implementations of singleton plugin of type '{requestedType.GetName()}' found: " +
            $"{matchingType1.GetName()} and {matchingType2.GetName()}.");

    public static Exception PluginIsAbstract(Type pluginType)
        => new InvalidOperationException($"Plugin '{pluginType}' is an abstract type.");
    public static Exception PluginIsNonPublic(Type pluginType)
        => new InvalidOperationException($"Plugin '{pluginType}' is a non-public type.");

    public static Exception PluginFinderRunFailed(Type pluginFinderType)
        => new InvalidOperationException($"'{pluginFinderType}.{nameof(IPluginFinder.Run)}()' method failed.");
}
