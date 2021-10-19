namespace Stl.Plugins;

// Implement it in your plugin to support capabilities extraction
// and filtering based on capabilities
public interface IHasCapabilities
{
    ImmutableOptionSet Capabilities { get; }
}
