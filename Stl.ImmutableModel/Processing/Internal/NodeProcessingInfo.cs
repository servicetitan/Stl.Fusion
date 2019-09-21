using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.ImmutableModel.Processing.Internal
{
    public class NodeProcessingInfo : INodeProcessingInfo, IDisposable
    { 
        public INodeProcessor NodeProcessor { get; set; }
        public DomainKey NodeDomainKey { get; set; }
        public SymbolPath NodePath { get; set; }
        public CancellationTokenSource NodeRemovedTokenSource { get; set; }
        public CancellationTokenSource ProcessStoppedOrNodeRemovedTokenSource { get; set; }
        public TaskCompletionSource<Unit> CompletionSource { get; set; }
        public Exception? Error { get; set; }
        public bool IsStartedForAlreadyExistingNode { get; set; }
        public bool IsDormant { get; set; }

        CancellationToken INodeProcessingInfo.ProcessStoppingToken => NodeProcessor.StoppingToken;
        CancellationToken INodeProcessingInfo.NodeRemovedToken => NodeRemovedTokenSource.Token;
        CancellationToken INodeProcessingInfo.ProcessStoppingOrNodeRemovedToken => ProcessStoppedOrNodeRemovedTokenSource.Token;

        public NodeProcessingInfo(
            INodeProcessor processor, 
            DomainKey nodeDomainKey, 
            SymbolPath nodePath,
            bool isStartedForAlreadyExistingNode)
        {
            NodeProcessor = processor;
            NodeDomainKey = nodeDomainKey;
            NodePath = nodePath;
            IsStartedForAlreadyExistingNode = isStartedForAlreadyExistingNode;
            NodeRemovedTokenSource = new CancellationTokenSource();
            ProcessStoppedOrNodeRemovedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                processor.StoppingToken, NodeRemovedTokenSource.Token);
            CompletionSource = new TaskCompletionSource<Unit>();
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
