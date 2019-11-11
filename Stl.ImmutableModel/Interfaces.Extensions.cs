using System.Collections.Generic;

namespace Stl.ImmutableModel 
{
    public interface IExtendableNode : ISimpleNode
    {
        // NOTE: This method must delete the extension when the value is null 
        IExtendableNode BaseWithExt(Symbol extension, object? value);
        IExtendableNode BaseWithAllExt(IEnumerable<(Symbol Extension, object? Value)> extensions);
    }

    public interface IExtensionNode : INode
    { }
}
