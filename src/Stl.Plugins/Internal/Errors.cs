using System;
using System.Collections.Generic;

namespace Stl.Plugins.Internal
{
    public static class Errors
    {
        public static Exception UnknownPluginImplementationType(Type pluginType)
            => throw new KeyNotFoundException($"Unknown plugin implementation type: '{pluginType.Name}'.");
        public static Exception PluginDisabled(Type pluginType)
            => throw new InvalidOperationException($"Plugin '{pluginType.Name}' is disabled.");

        public static Exception MultipleSingletonPluginImplementationsFound(
            Type requestedType, Type matchingType1, Type matchingType2)
            => throw new InvalidOperationException(
                $"Multiple implementations of singleton plugin of type '{requestedType.Name}' found: " +
                $"{matchingType1.Name} and {matchingType2.Name}."); 
    }
}
