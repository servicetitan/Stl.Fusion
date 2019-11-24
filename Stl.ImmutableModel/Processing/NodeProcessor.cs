using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Stl.Async;
using Stl.ImmutableModel.Indexing;
using Stl.ImmutableModel.Processing.Internal;
using Stl.ImmutableModel.Updating;
using Stl.Text;

namespace Stl.ImmutableModel.Processing
{
    // These types are intentionally non-generic: this allows
    // the same NodeProcessor to be used in different models.
    public interface INodeProcessor : IAsyncProcess
    {
        IModelProvider ModelProvider { get; }
    }

    public interface INodeProcessor<TModel> : INodeProcessor
        where TModel : class, INode
    {
        new IModelProvider<TModel> ModelProvider { get; }
    }

    public abstract class NodeProcessorBase : AsyncProcessBase, INodeProcessor
    {
        protected ConcurrentDictionary<Key, NodeProcessingInfo> Processes { get; } =
            new ConcurrentDictionary<Key, NodeProcessingInfo>();
        protected ConcurrentDictionary<NodeProcessingInfo, Unit> DyingProcesses { get; } =
            new ConcurrentDictionary<NodeProcessingInfo, Unit>();

        public IModelProvider ModelProvider { get; }
        public IModelIndex ModelIndex => ModelProvider.Index;
        public IModelChangeTracker ChangeTracker => ModelProvider.ChangeTracker;

        protected NodeProcessorBase(IModelProvider modelProvider) 
            => ModelProvider = modelProvider;

        protected abstract Task ProcessNodeAsync(INodeProcessingInfo info);
        protected virtual Task OnReadyAsync() => Task.CompletedTask;
        protected virtual bool IsSupportedUpdate(IModelUpdateInfo updateInfo) => true;
        protected virtual bool IsSupportedChange(in NodeChangeInfo nodeChangeInfo) => true; 

        protected override async Task RunInternalAsync()
        {
            var allChanges = ChangeTracker.AllChanges.Publish(); 
            var processorTask = allChanges.Select(ProcessChange).ToTask(StoppingToken).SuppressCancellation();
            using (allChanges.Connect()) {
                await OnReadyAsync().ConfigureAwait(false);
                StartProcessesForExistingNodes();
                await processorTask.ConfigureAwait(false);
            }

            // If we're here, there are two options:
            // - StopToken is cancelled 
            // - AllChanges sequence is exhausted b/c ChangeTracker was disposed.
            // We should handle both cases properly. So:
            
            // 1. We cancel the main token (~ process stopping event)
            if (!StoppingTokenSource.IsCancellationRequested)
                StoppingTokenSource.Cancel();
            
            // 2. We wait till both Processes and DyingProcesses deplete
            while (true) {
                var (domainKey, info) = Processes.FirstOrDefault();
                if (info == null)
                    break;
                await info.CompletionSource.Task.ConfigureAwait(false);
                Processes.TryRemove(domainKey, out _);
            }
            while (true) {
                var (info, _) = DyingProcesses.FirstOrDefault();
                if (info == null)
                    break;
                await info.CompletionSource.Task.ConfigureAwait(false);
                DyingProcesses.TryRemove(info, out _);
            }
        }

        protected virtual void StartProcessesForExistingNodes()
        {
            var modelType = ModelProvider.GetModelType();
            var index = ModelProvider.Index;

            var modelUpdateInfoType = typeof(ModelUpdateInfo<>).MakeGenericType(modelType);
            var modelUpdateInfo = (IModelUpdateInfo) Activator.CreateInstance(
                modelUpdateInfoType, index, index, ModelChangeSet.Empty)!;
            
            var changes = index.Entries
                .Select(item => new NodeChangeInfo(modelUpdateInfo, item.Node, item.NodeLink, 0))
                .Where(info => IsSupportedChange(info))
                .ToList();
            foreach (var nodeChange in OrderChanges(changes))
                ProcessNodeChange(nodeChange);
        }

        protected virtual Unit ProcessChange(IModelUpdateInfo updateInfo)
        {
            if (!IsSupportedUpdate(updateInfo))
                return default;
            var newIndex = updateInfo.NewModelIndex;
            var oldIndex = updateInfo.OldModelIndex;
            var changes = new List<NodeChangeInfo>();
            foreach (var (domainKey, changeType) in updateInfo.ChangeSet) {
                INode node;
                NodeLink nodeLink;
                if (changeType.HasFlag(NodeChangeType.Removed)) {
                    node = oldIndex.GetNode(domainKey);
                    nodeLink = oldIndex.GetNodeLink(node);
                }
                else {
                    node = newIndex.GetNode(domainKey);
                    nodeLink = newIndex.GetNodeLink(node);
                }
                var nodeChangeInfo = new NodeChangeInfo(updateInfo, node, nodeLink, changeType);
                if (IsSupportedChange(nodeChangeInfo))
                    changes.Add(nodeChangeInfo);
            }
            foreach (var nodeChange in OrderChanges(changes))
                ProcessNodeChange(nodeChange);
            return default;
        }

        protected virtual IEnumerable<NodeChangeInfo> OrderChanges(List<NodeChangeInfo> changes) 
            // Removals go fist, then creations, and finally, all other changes
            => changes.OrderBy(c => (int) c.ChangeType);

        protected virtual void ProcessNodeChange(NodeChangeInfo nodeChangeInfo)
        {
            var domainKey = nodeChangeInfo.Node.Key;
            var nodeProcessingInfo = Processes.GetValueOrDefault(domainKey);
            var nodeChangeType = nodeChangeInfo.ChangeType;
            if (nodeProcessingInfo == null) {
                if (nodeChangeType.HasFlag(NodeChangeType.Removed))
                    // Nothing to do: node is removed & the process isn't running
                    return;
                nodeProcessingInfo = new NodeProcessingInfo(
                    this, domainKey, nodeChangeInfo.NodeLink, 
                    !nodeChangeType.HasFlag(NodeChangeType.Created));
                var existingInfo = Processes.GetOrAdd(domainKey, nodeProcessingInfo);
                if (existingInfo != nodeProcessingInfo) {
                    // Some other thread just created the task somehow
                    nodeProcessingInfo.Dispose();
                    nodeProcessingInfo = existingInfo;
                }
                else
                    Task.Run(() => WrapProcessNodeAsync(nodeProcessingInfo));
            }
            if (nodeChangeType.HasFlag(NodeChangeType.Removed)) {
                // To avoid race conditions, we have to move the process to DyingProcesses first
                if (Processes.TryRemove(nodeProcessingInfo.NodeKey, out _))
                    DyingProcesses.TryAdd(nodeProcessingInfo, default);
                // And now it's safe to trigger the events that might cause process termination
                nodeProcessingInfo.NodeRemovedTokenSource.Cancel();
            }
        }

        protected virtual async Task WrapProcessNodeAsync(NodeProcessingInfo info)
        {
            try {
                await ProcessNodeAsync(info).ConfigureAwait(false);
            }
            catch (Exception e) {
                info.Error = e;
            }
            finally {
                await CompleteProcessNodeAsync(info);
            }
        }

        protected async Task CompleteProcessNodeAsync(NodeProcessingInfo info)
        {
            // The "termination tail"
            if (!info.ProcessStoppedOrNodeRemovedTokenSource.IsCancellationRequested) {
                // Let's wait for the "official" cancellation first - we can't
                // remove the process from Processes just yet, b/c otherwise
                // it will be re-created on the next event.
                info.IsDormant = true;
                await info.ProcessStoppedOrNodeRemovedTokenSource.Token.ToTask(false).ConfigureAwait(false);
            }
            Processes.TryRemove(info.NodeKey, out _);
            DyingProcesses.TryRemove(info, out _);
            info.Dispose();
            info.CompletionSource.SetResult(default);
        }
    }

    public abstract class NodeProcessorBase<TModel> : NodeProcessorBase
        where TModel : class, INode
    {
        public new IModelProvider<TModel> ModelProvider { get; }
        public new IModelIndex<TModel> ModelIndex => ModelProvider.Index;
        public new IModelChangeTracker<TModel> ChangeTracker => ModelProvider.ChangeTracker;

        protected NodeProcessorBase(IModelProvider<TModel> modelProvider) : base(modelProvider) 
            => ModelProvider = modelProvider;
    }
}
