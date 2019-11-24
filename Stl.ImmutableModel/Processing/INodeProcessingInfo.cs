using System.Threading;
using Stl.ImmutableModel.Indexing;
using Stl.Text;

namespace Stl.ImmutableModel.Processing
{
    public interface INodeProcessingInfo
    {
        INodeProcessor NodeProcessor { get; }
        // There is no Node member -- intentionally, otherwise such nodes
        // will be for sure held while the process created for them is running
        Key NodeKey { get; }
        NodeLink NodeLink { get; }
        CancellationToken ProcessStoppingToken { get; }
        CancellationToken NodeRemovedToken { get; }
        CancellationToken ProcessStoppingOrNodeRemovedToken { get; }
        bool IsStartedForAlreadyExistingNode { get; }
    }
}
