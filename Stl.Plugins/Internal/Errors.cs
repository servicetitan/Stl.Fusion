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
    }
}
