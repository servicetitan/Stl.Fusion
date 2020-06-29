using System;

namespace Stl.Plugins
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class PluginAttribute : Attribute
    {
        public Type Type { get; }

        public PluginAttribute(Type type) => Type = type;
    }
}
