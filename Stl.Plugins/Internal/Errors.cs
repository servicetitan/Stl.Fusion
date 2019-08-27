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

        public static Exception CantUsePluginsTogetherWithPluginTypes()
            => throw new InvalidOperationException($"Can't use Plugins and PluginTypes together.");
    }
}
