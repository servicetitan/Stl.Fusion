
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Extensibility;
using Stl.ImmutableModel.Indexing;
using Stl.ImmutableModel.Processing.Internal;
using Stl.ImmutableModel.Updating;
using Stl.Internal;

namespace Stl.ImmutableModel.Processing
{
    public interface INodeProcessor : IAsyncProcess
    {
        IModelProvider UntypedModelProvider { get; }
        IUpdater UntypedUpdater { get; }
        DomainKey RootKey { get; }
    }

    public interface INodeProcessor<TModel> : INodeProcessor
        where TModel : class, INode
    {
        IModelProvider<TModel> ModelProvider { get; }
        IUpdater<TModel> Updater { get; }
    }

    public abstract class NodeProcessorBase<TModel> : AsyncProcessBase, INodeProcessor<TModel>
        where TModel : class, INode
    {
        protected ConcurrentDictionary<DomainKey, NodeProcessingInfo<TModel>> Processes { get; } =
            new ConcurrentDictionary<DomainKey, NodeProcessingInfo<TModel>>();
        protected ConcurrentDictionary<NodeProcessingInfo<TModel>, Unit> DyingProcesses { get; } =
            new ConcurrentDictionary<NodeProcessingInfo<TModel>, Unit>();

        public IModelProvider<TModel> ModelProvider { get; }
        public IUpdater<TModel> Updater { get; }
        public DomainKey RootKey { get; }

        IModelProvider INodeProcessor.UntypedModelProvider => ModelProvider;
        IUpdater INodeProcessor.UntypedUpdater => Updater;

        protected NodeProcessorBase(
            IModelProvider<TModel> modelProvider, 
            IUpdater<TModel> updater, 
            DomainKey rootKey)
        {
            ModelProvider = modelProvider;
            Updater = updater;
            RootKey = rootKey;
        }

        protected override async Task RunInternalAsync()
        {
            var allChanges = ModelProvider.ChangeTracker.AllChanges.Publish(); 
            var processorTask = ModelProvider.ChangeTracker.AllChanges.Select(ProcessChange).ToTask(StoppingToken).SuppressCancellation();
            using (allChanges.Connect()) {
                await OnReadyAsync().ConfigureAwait(false);
                await processorTask.ConfigureAwait(false);
            }

            // If we're here, there are two opitons:
            // - StopToken is cancelled 
            // - AllChanges sequence is exhausted b/c ChangeTracker was disposed.
            // We should handle both cases properly. So:
            
            // 1. We cancel the main token (~ process stopping event)
            if (!StoppingTokenSource.IsCancellationRequested)
                StoppingTokenSource.Cancel();
            
            // 2. We indicate there are no more events in Changes sequence
            foreach (var (_, processInfo) in Processes)
                processInfo.Changes.OnCompleted();

            // 4. We wait till both Processes and DyingProcesses deplete
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

        protected Unit ProcessChange(UpdateInfo<TModel> updateInfo)
        {
            var newIndex = updateInfo.NewIndex;
            var oldIndex = updateInfo.OldIndex;
            var rootNode = newIndex.GetNode(RootKey);
            var rootPath = newIndex.GetPath(rootNode);
            if (!updateInfo.ChangeSet.Changes.ContainsKey(rootNode.DomainKey))
                // Some other subtree is changed.
                return default;

            var changes = new List<NodeChangeInfo<TModel>>();
            foreach (var (domainKey, changeType) in updateInfo.ChangeSet.Changes) {
                if (changeType.HasFlag(NodeChangeType.Removed)) {
                    var node = oldIndex.GetNode(domainKey);
                    var path = oldIndex.GetPath(node);
                    if (path.StartsWith(rootPath))
                        changes.Add(new NodeChangeInfo<TModel>(updateInfo, node, path, changeType));
                }
                else {
                    var node = newIndex.GetNode(domainKey);
                    var path = newIndex.GetPath(node);
                    if (path.StartsWith(rootPath))
                        changes.Add(new NodeChangeInfo<TModel>(updateInfo, node, path, changeType));
                }
            }
            foreach (var nodeChange in changes.OrderBy(c => c.Path.SegmentCount))
                ProcessNodeChange(nodeChange);
            return default;
        }

        protected virtual void ProcessNodeChange(NodeChangeInfo<TModel> nodeChangeInfo)
        {
            var domainKey = nodeChangeInfo.Node.DomainKey;
            var info = Processes.GetValueOrDefault(domainKey);
            if (info == null) {
                var isNewlyCreatedNode = nodeChangeInfo.ChangeType.HasFlag(NodeChangeType.Created);
                info = new NodeProcessingInfo<TModel>(this, domainKey, nodeChangeInfo.Path, isNewlyCreatedNode);
                if (!Processes.TryAdd(domainKey, info))
                    throw Errors.InternalError($"The task is already added to {nameof(Processes)}.");
                Task.Run(() => ProcessNodeAsync(info)).ContinueWith((task, state) => {
                    var info1 = (NodeProcessingInfo<TModel>) state!;
                    var self = (NodeProcessorBase<TModel>) info1.Processor;
                    try {
                        // This is the "termination tail" for every process.
                        self.Processes.TryRemove(info1.NodeDomainKey, out _);
                        self.DyingProcesses.TryRemove(info1, out _);
                        info1.Dispose();
                    }
                    finally {
                        info1.CompletionSource.SetResult(default);
                    }
                }, info);
            }
            if (nodeChangeInfo.ChangeType.HasFlag(NodeChangeType.Removed)) {
                // To avoid race conditions, wwe have to move the process to DyingProcesses first
                if (Processes.TryRemove(info.NodeDomainKey, out _))
                    DyingProcesses.TryAdd(info, default);
                // And now it's safe to trigger the events that might cause process termination
                info.Changes.OnNext(nodeChangeInfo);
                info.Changes.OnCompleted();
                info.NodeRemovedTokenSource.Cancel();
                return;
            }
            info.Changes.OnNext(nodeChangeInfo);
        }

        protected abstract Task ProcessNodeAsync(INodeProcessingInfo<TModel> info);

        protected virtual Task OnReadyAsync() 
            => Task.CompletedTask;
    }

    public class NodeProcessor<TModel> : NodeProcessorBase<TModel>
        where TModel : class, INode
    {
        public Func<INodeProcessingInfo<TModel>, Task> Implementation { get; }

        public NodeProcessor(
            IModelProvider<TModel> modelProvider, 
            IUpdater<TModel> updater, 
            DomainKey rootKey, 
            Func<INodeProcessingInfo<TModel>, Task> implementation) 
            : base(modelProvider, updater, rootKey) 
            => Implementation = implementation;

        protected override Task ProcessNodeAsync(INodeProcessingInfo<TModel> info) 
            => Implementation.Invoke(info);
    }

    public static class ModelProcessor
    {
        public static NodeProcessor<TModel> New<TModel>(
            IModelProvider<TModel> modelProvider, 
            IUpdater<TModel> updater, 
            DomainKey rootKey, 
            Func<INodeProcessingInfo<TModel>, Task> implementation) 
            where TModel : class, INode
            => new NodeProcessor<TModel>(modelProvider, updater, rootKey, implementation);
    }
}
