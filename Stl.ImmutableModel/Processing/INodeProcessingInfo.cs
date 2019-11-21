using System.Threading;
using Stl.Text;

namespace Stl.ImmutableModel.Processing
{
    public interface INodeProcessingInfo
    {
        INodeProcessor NodeProcessor { get; }
        // There is no Node member -- intentionally, otherwise such nodes
        // will be for sure held while the process created for them is running
        Key NodeKey { get; }
        SymbolList NodePath { get; }
        CancellationToken ProcessStoppingToken { get; }
        CancellationToken NodeRemovedToken { get; }
        CancellationToken ProcessStoppingOrNodeRemovedToken { get; }
        bool IsStartedForAlreadyExistingNode { get; }
    }
}
