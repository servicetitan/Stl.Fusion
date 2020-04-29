using System;
using System.Threading.Tasks;
using Stl.ImmutableModel.Updating;

namespace Stl.ImmutableModel.Processing 
{
    public class SimpleNodeProcessor : NodeProcessorBase
    {
        public Func<INodeProcessingInfo, Task> Implementation { get; }
        public Func<IModelUpdateInfo, bool>? IsSupportedUpdatePredicate { get; }
        public Func<NodeChangeInfo, bool>? IsSupportedChangePredicate { get; }

        public SimpleNodeProcessor(
            IModelProvider modelProvider, 
            Func<INodeProcessingInfo, Task> implementation,
            Func<IModelUpdateInfo, bool>? isSupportedUpdatePredicate = null,
            Func<NodeChangeInfo, bool>? isSupportedChangePredicate = null) 
            : base(modelProvider)
        {
            Implementation = implementation;
            IsSupportedUpdatePredicate = isSupportedUpdatePredicate;
            IsSupportedChangePredicate = isSupportedChangePredicate;
        }

        protected override Task ProcessNodeAsync(INodeProcessingInfo info) 
            => Implementation.Invoke(info);
        protected override bool IsSupportedUpdate(IModelUpdateInfo updateInfo) 
            => IsSupportedUpdatePredicate?.Invoke(updateInfo) ?? true;
        protected override bool IsSupportedChange(in NodeChangeInfo nodeChangeInfo) 
            => IsSupportedChangePredicate?.Invoke(nodeChangeInfo) ?? true;
    }
}
