namespace Stl.Plugins;

#pragma warning disable CA1813 // Consider making sealed

[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public bool IsEnabled { get; set; } = true;
}
