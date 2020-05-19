using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.ImmutableModel.Indexing;

namespace Stl.ImmutableModel.Processing.Internal
{
    public class NodeProcessingInfo : INodeProcessingInfo, IDisposable
    { 
        public INodeProcessor NodeProcessor { get; set; }
        public Key NodeKey { get; set; }
        public NodeLink NodeLink { get; set; }
        public CancellationTokenSource NodeRemovedTokenSource { get; set; }
        public CancellationTokenSource ProcessStoppedOrNodeRemovedTokenSource { get; set; }
        public TaskSource<Unit> CompletionSource { get; set; }
        public Exception? Error { get; set; }
        public bool IsStartedForAlreadyExistingNode { get; set; }
        public bool IsDormant { get; set; }

        CancellationToken INodeProcessingInfo.ProcessStoppingToken => NodeProcessor.StopToken;
        CancellationToken INodeProcessingInfo.NodeRemovedToken => NodeRemovedTokenSource.Token;
        CancellationToken INodeProcessingInfo.ProcessStoppingOrNodeRemovedToken => ProcessStoppedOrNodeRemovedTokenSource.Token;

        public NodeProcessingInfo(
            INodeProcessor processor, 
            Key nodeKey, 
            NodeLink nodeLink,
            bool isStartedForAlreadyExistingNode)
        {
            NodeProcessor = processor;
            NodeKey = nodeKey;
            NodeLink = nodeLink;
            IsStartedForAlreadyExistingNode = isStartedForAlreadyExistingNode;
            NodeRemovedTokenSource = new CancellationTokenSource();
            ProcessStoppedOrNodeRemovedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                processor.StopToken, NodeRemovedTokenSource.Token);
            CompletionSource = TaskSource.New<Unit>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        protected virtual void Dispose(bool disposing)
        {
            NodeRemovedTokenSource?.Dispose();
            ProcessStoppedOrNodeRemovedTokenSource?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
