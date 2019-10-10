using System.Reflection;
using System.Threading.Tasks;
using Stl.ImmutableModel.Indexing;
using Stl.ImmutableModel.Updating;
using Stl.Reflection;

namespace Stl.ImmutableModel.Processing 
{
    public abstract class DispatchingNodeProcessorBase : NodeProcessorBase
    {
        protected DispatchingNodeProcessorBase(IModelProvider modelProvider) 
            : base(modelProvider) { }

        protected override async Task ProcessNodeAsync(INodeProcessingInfo info)
        {
            var methodName = GetMethodName(info);
            var method = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) {
                await ProcessUnknownNodeAsync(info).ConfigureAwait(false);
                return;
            }
            var task = (Task) method.Invoke(this, new object[] {info})!;
            await task.ConfigureAwait(false);
        }

        protected virtual string GetMethodName(INodeProcessingInfo info)
        {
            var node = ModelProvider.Index.GetNode(info.NodeKey);
            var type = node.GetType();
            return $"Process{type.ToMethodName()}NodeAsync";
        }

        protected virtual Task ProcessUnknownNodeAsync(INodeProcessingInfo info) => Task.CompletedTask;
    }

    public abstract class DispatchingNodeProcessorBase<TModel> : DispatchingNodeProcessorBase
        where TModel : class, INode
    {
        public new IModelProvider<TModel> ModelProvider { get; }
        public new IIndex<TModel> Index => ModelProvider.Index;
        public new IModelChangeTracker<TModel> ChangeTracker => ModelProvider.ChangeTracker;

        protected DispatchingNodeProcessorBase(IModelProvider<TModel> modelProvider) : base(modelProvider)
        {
            ModelProvider = modelProvider;
        }
    }
}
