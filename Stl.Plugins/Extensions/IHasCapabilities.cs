using System.Collections.Immutable;

namespace Stl.Plugins.Extensions
{
    public interface IHasCapabilities
    {
        ImmutableDictionary<string, object> Capabilities { get; }
    }
}
