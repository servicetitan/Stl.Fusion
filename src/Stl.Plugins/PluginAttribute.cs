namespace Stl.Plugins;

[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public bool IsEnabled { get; set; } = true;
}
