using System;
using System.Threading;

namespace Stl.ImmutableModel.Processing
{
    public interface INodeProcessingInfo
    {
        INodeProcessor NodeProcessor { get; }
        DomainKey NodeDomainKey { get; }
        SymbolPath NodePath { get; }
        CancellationToken ProcessStoppingToken { get; }
        CancellationToken NodeRemovedToken { get; }
        CancellationToken ProcessStoppingOrNodeRemovedToken { get; }
        bool IsStartedForAlreadyExistingNode { get; }
    }
}
