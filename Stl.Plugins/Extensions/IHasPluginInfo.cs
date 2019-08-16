using System.Collections.Immutable;

namespace Stl.Plugins.Extensions
{
    public interface IHasCapabilities
    {
        ImmutableHashSet<string> Capabilities { get; }
    }
}
