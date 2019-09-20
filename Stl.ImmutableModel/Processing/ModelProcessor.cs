
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
using Stl.ImmutableModel.Indexing;
using Stl.ImmutableModel.Updating;
using Stl.Internal;

namespace Stl.ImmutableModel.Processing
{
    public abstract class ModelProcessorBase<TModel> : AsyncProcessBase
        where TModel : class, INode
    {
        protected Dictionary<DomainKey, (Task Task, Subject<NodeChangeInfo<TModel>> Changes)> Processes { get; } =
            new Dictionary<DomainKey, (Task Task, Subject<NodeChangeInfo<TModel>> Changes)>();
        protected ConcurrentDictionary<Task, Subject<NodeChangeInfo<TModel>>> DyingProcesses { get; } =
            new ConcurrentDictionary<Task, Subject<NodeChangeInfo<TModel>>>();

        public IModelProvider<TModel> ModelProvider { get; }
        public IUpdater<TModel> Updater { get; }
        public DomainKey RootKey { get; }

        protected ModelProcessorBase(
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
            await ModelProvider.ChangeTracker.AllChanges.Select(ProcessChange).ToTask(StopToken);
            foreach (var (domainKey, (task, changes)) in Processes)
                changes.OnCompleted();
            while (true) {
                var (task, _) = DyingProcesses.FirstOrDefault();
                if (task == null)
                    break;
                await task;
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
            var nodeDomainKey = nodeChangeInfo.Node.DomainKey;
            var (task, changes) = Processes.GetValueOrDefault(nodeDomainKey);
            if (task == null) {
                if (!nodeChangeInfo.ChangeType.HasFlag(NodeChangeType.Created))
                    HandleMissingProcess(nodeChangeInfo);
                changes = new Subject<NodeChangeInfo<TModel>>();
                task = ProcessNodeAsync(changes);
                Processes.Add(nodeDomainKey, (task, changes));
            }
            changes.OnNext(nodeChangeInfo);
            if (nodeChangeInfo.ChangeType.HasFlag(NodeChangeType.Removed)) {
                changes.OnCompleted();
                Processes.Remove(nodeDomainKey);
                if (!DyingProcesses.TryAdd(task, changes))
                    throw Errors.InternalError($"The task is already added to {nameof(DyingProcesses)}.");
                task.ContinueWith((task1, state) => {
                    var self = (ModelProcessorBase<TModel>) state!;
                    if (!self.DyingProcesses.Remove(task1, out var changes1))
                        throw Errors.InternalError($"The task is already removed from {nameof(DyingProcesses)}.");
                    changes1.Dispose();
                }, this);
            }
        }

        protected abstract Task ProcessNodeAsync(Subject<NodeChangeInfo<TModel>> changes);

        protected virtual void HandleMissingProcess(NodeChangeInfo<TModel> nodeChangeInfo) 
            => throw Errors.InternalError(
                "Node change event is triggered for a node w/o associated process.");
    }

    public class ModelProcessor<TModel> : ModelProcessorBase<TModel>
        where TModel : class, INode
    {
        public Func<Subject<NodeChangeInfo<TModel>>, CancellationToken, Task> NodeProcessor { get; }

        public ModelProcessor(
            IModelProvider<TModel> modelProvider, 
            IUpdater<TModel> updater, 
            DomainKey rootKey, 
            Func<Subject<NodeChangeInfo<TModel>>, CancellationToken, Task> nodeProcessor) 
            : base(modelProvider, updater, rootKey) 
            => NodeProcessor = nodeProcessor;

        protected override Task ProcessNodeAsync(Subject<NodeChangeInfo<TModel>> changes) 
            => NodeProcessor(changes, StopToken);
    }

    public static class ModelProcessor
    {
        public static ModelProcessor<TModel> New<TModel>(
            IModelProvider<TModel> modelProvider, 
            IUpdater<TModel> updater, 
            DomainKey rootKey, 
            Func<Subject<NodeChangeInfo<TModel>>, CancellationToken, Task> nodeProcessor) 
            where TModel : class, INode
            => new ModelProcessor<TModel>(modelProvider, updater, rootKey, nodeProcessor);
    }
}
