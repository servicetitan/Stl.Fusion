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

namespace Stl.ImmutableModel.Processing
{
    // These types are intentionally non-generic: this allows
    // the same NodeProcessor to be used in different models.
    public interface INodeProcessor : IAsyncProcess
    {
        IModelProvider ModelProvider { get; }
        IUpdater Updater { get; }
    }

    public abstract class NodeProcessorBase : AsyncProcessBase, INodeProcessor
    {
        protected ConcurrentDictionary<Key, NodeProcessingInfo> Processes { get; } =
            new ConcurrentDictionary<Key, NodeProcessingInfo>();
        protected ConcurrentDictionary<NodeProcessingInfo, Unit> DyingProcesses { get; } =
            new ConcurrentDictionary<NodeProcessingInfo, Unit>();

        public IModelProvider ModelProvider { get; }
        public IUpdater Updater { get; }

        protected NodeProcessorBase(
            IModelProvider modelProvider, 
            IUpdater updater)
        {
            ModelProvider = modelProvider;
            Updater = updater;
        }

        protected abstract Task ProcessNodeAsync(INodeProcessingInfo info);
        protected virtual Task OnReadyAsync() => Task.CompletedTask;
        protected virtual bool IsSupportedUpdate(IUpdateInfo updateInfo) => true;
        protected virtual bool IsSupportedChange(in NodeChangeInfo nodeChangeInfo) => true; 

        protected override async Task RunInternalAsync()
        {
            var allChanges = ModelProvider.ChangeTracker.AllChanges.Publish(); 
            var processorTask = allChanges.Select(ProcessChange).ToTask(StoppingToken).SuppressCancellation();
            using (allChanges.Connect()) {
                await OnReadyAsync().ConfigureAwait(false);
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

        protected Unit ProcessChange(IUpdateInfo updateInfo)
        {
            if (!IsSupportedUpdate(updateInfo))
                return default;
            var newIndex = updateInfo.NewIndex;
            var oldIndex = updateInfo.OldIndex;
            var changes = new List<NodeChangeInfo>();
            foreach (var (domainKey, changeType) in updateInfo.ChangeSet.Changes) {
                INode node;
                SymbolList path;
                if (changeType.HasFlag(NodeChangeType.Removed)) {
                    node = oldIndex.GetNode(domainKey);
                    path = oldIndex.GetPath(node);
                }
                else {
                    node = newIndex.GetNode(domainKey);
                    path = newIndex.GetPath(node);
                }
                var nodeChangeInfo = new NodeChangeInfo(updateInfo, node, path, changeType);
                if (IsSupportedChange(nodeChangeInfo))
                    changes.Add(nodeChangeInfo);
            }
            foreach (var nodeChange in OrderChanges(changes))
                ProcessNodeChange(nodeChange);
            return default;
        }

        protected virtual IEnumerable<NodeChangeInfo> OrderChanges(List<NodeChangeInfo> changes) 
            => changes.OrderBy(c => (
                // Removals go fist, then creations, and finally, other changes
                (int) c.ChangeType,
                // For newly created nodes, we start from the ones closer to the root;
                // for other nodes, we start from the deepest ones.
                c.ChangeType.HasFlag(NodeChangeType.Created) ? c.NodePath.SegmentCount : -c.NodePath.SegmentCount));

        protected virtual void ProcessNodeChange(NodeChangeInfo nodeChangeInfo)
        {
            var domainKey = nodeChangeInfo.Node.Key;
            var nodeProcessingInfo = Processes.GetValueOrDefault(domainKey);
            if (nodeProcessingInfo == null) {
                nodeProcessingInfo = new NodeProcessingInfo(
                    this, domainKey, nodeChangeInfo.NodePath, 
                    !nodeChangeInfo.ChangeType.HasFlag(NodeChangeType.Created));
                var existingInfo = Processes.GetOrAdd(domainKey, nodeProcessingInfo);
                if (existingInfo != nodeProcessingInfo) {
                    // Some other thread just created the task somehow
                    nodeProcessingInfo.Dispose();
                    nodeProcessingInfo = existingInfo;
                }
                else
                    Task.Run(() => WrapProcessNodeAsync(nodeProcessingInfo));
            }
            if (nodeChangeInfo.ChangeType.HasFlag(NodeChangeType.Removed)) {
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
    }

    public class NodeProcessor : NodeProcessorBase
    {
        public Func<INodeProcessingInfo, Task> Implementation { get; }
        public Func<IUpdateInfo, bool>? IsSupportedUpdatePredicate { get; }
        public Func<NodeChangeInfo, bool>? IsSupportedChangePredicate { get; }

        public NodeProcessor(
            IModelProvider modelProvider, 
            IUpdater updater, 
            Func<INodeProcessingInfo, Task> implementation,
            Func<IUpdateInfo, bool>? isSupportedUpdatePredicate = null,
            Func<NodeChangeInfo, bool>? isSupportedChangePredicate = null) 
            : base(modelProvider, updater)
        {
            Implementation = implementation;
            IsSupportedUpdatePredicate = isSupportedUpdatePredicate;
            IsSupportedChangePredicate = isSupportedChangePredicate;
        }

        protected override Task ProcessNodeAsync(INodeProcessingInfo info) 
            => Implementation.Invoke(info);
        protected override bool IsSupportedUpdate(IUpdateInfo updateInfo) 
            => IsSupportedUpdatePredicate?.Invoke(updateInfo) ?? true;
        protected override bool IsSupportedChange(in NodeChangeInfo nodeChangeInfo) 
            => IsSupportedChangePredicate?.Invoke(nodeChangeInfo) ?? true;
    }
}
