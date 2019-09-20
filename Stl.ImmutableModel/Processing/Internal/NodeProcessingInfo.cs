using System;
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
        public Subject<NodeChangeInfo<TModel>> Changes { get; set; }  
        public CancellationTokenSource NodeRemovedCts { get; set; }
        public CancellationTokenSource ProcessStoppedOrNodeRemovedCts { get; set; }
        public Task Task { get; set; }

        INodeProcessor INodeProcessingInfo.UntypedProcessor => Processor;
        IObservable<NodeChangeInfo> INodeProcessingInfo.UntypedChanges => Changes;
        IObservable<NodeChangeInfo<TModel>> INodeProcessingInfo<TModel>.Changes => Changes;
        CancellationToken INodeProcessingInfo.ProcessStoppingToken => Processor.StoppingToken;
        CancellationToken INodeProcessingInfo.NodeRemovedToken => NodeRemovedCts.Token;
        CancellationToken INodeProcessingInfo.ProcessStoppingOrNodeRemovedToken => ProcessStoppedOrNodeRemovedCts.Token;

        protected virtual void Dispose(bool disposing)
        {
            Changes?.Dispose();
            NodeRemovedCts?.Dispose();
            ProcessStoppedOrNodeRemovedCts?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
