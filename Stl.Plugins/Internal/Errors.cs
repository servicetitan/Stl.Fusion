using System;
using System.Collections.Generic;

namespace Stl.Plugins.Internal
{
    public static class Errors
    {
        public static Exception UnknownPluginImplementationType(string implementationType)
            => throw new KeyNotFoundException($"Unknown plugin implementation type: {implementationType}.");
        
        public static Exception UnknownPluginType(string pluginType)
            => throw new KeyNotFoundException($"Unknown plugin type: {pluginType}.");

        public static Exception CantCreatePluginInstance(string pluginTypeName)
            => throw new InvalidOperationException($"Can't create \"{pluginTypeName}\" instance.");

        public static Exception CantUsePluginConfigurationWithPluginTypes()
            => throw new InvalidOperationException($"Can't use both PluginConfiguration and PluginTypes.");
    }
}
