using System;
using System.Threading;

namespace Stl.ImmutableModel.Processing
{
    public interface INodeProcessingInfo
    {
        INodeProcessor UntypedProcessor { get; }

        DomainKey NodeDomainKey { get; }
        SymbolPath NodePath { get; }
        CancellationToken ProcessStoppingToken { get; }
        CancellationToken NodeRemovedToken { get; }
        CancellationToken ProcessStoppingOrNodeRemovedToken { get; }
        bool IsStartedForAlreadyExistingNode { get; }
    }

    public interface INodeProcessingInfo<TModel> : INodeProcessingInfo
        where TModel : class, INode
    {
        INodeProcessor<TModel> Processor { get; }
    }
}
