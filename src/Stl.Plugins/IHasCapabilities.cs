using System.Collections.Immutable;

namespace Stl.Plugins
{
    // Implement it in your plugin to support capabilities extraction
    // and filtering based on capabilities
    public interface IHasCapabilities
    {
        ImmutableDictionary<string, object> Capabilities { get; }
    }
}
