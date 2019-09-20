using System;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.ImmutableModel.Processing.Internal
{
    public class NodeProcessingInfo<TModel> : INodeProcessingInfo<TModel>, IDisposable
        where TModel : class, INode
    { 
        public INodeProcessor<TModel> Processor { get; set; }
        public DomainKey NodeDomainKey { get; set; }
        public SymbolPath NodePath { get; set; }
        public bool IsNewlyCreatedNode { get; set; }
        public Subject<NodeChangeInfo<TModel>> Changes { get; set; }  
        public CancellationTokenSource NodeRemovedTokenSource { get; set; }
        public CancellationTokenSource ProcessStoppedOrNodeRemovedTokenSource { get; set; }
        public TaskCompletionSource<Unit> CompletionSource { get; set; }

        INodeProcessor INodeProcessingInfo.UntypedProcessor => Processor;
        IObservable<NodeChangeInfo> INodeProcessingInfo.UntypedChanges => Changes;
        IObservable<NodeChangeInfo<TModel>> INodeProcessingInfo<TModel>.Changes => Changes;
        CancellationToken INodeProcessingInfo.ProcessStoppingToken => Processor.StoppingToken;
        CancellationToken INodeProcessingInfo.NodeRemovedToken => NodeRemovedTokenSource.Token;
        CancellationToken INodeProcessingInfo.ProcessStoppingOrNodeRemovedToken => ProcessStoppedOrNodeRemovedTokenSource.Token;

        public NodeProcessingInfo(
            INodeProcessor<TModel> processor, 
            DomainKey nodeDomainKey, 
            SymbolPath nodePath,
            bool isNewlyCreatedNode)
        {
            Processor = processor;
            NodeDomainKey = nodeDomainKey;
            NodePath = nodePath;
            IsNewlyCreatedNode = isNewlyCreatedNode;
            Changes = new Subject<NodeChangeInfo<TModel>>();
            NodeRemovedTokenSource = new CancellationTokenSource();
            ProcessStoppedOrNodeRemovedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                processor.StoppingToken, NodeRemovedTokenSource.Token);
            CompletionSource = new TaskCompletionSource<Unit>();
        }

        protected virtual void Dispose(bool disposing)
        {
            Changes?.Dispose();
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
