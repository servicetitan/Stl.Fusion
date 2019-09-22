using System.Reflection;
using System.Threading.Tasks;
using Stl.ImmutableModel.Updating;
using Stl.Reflection;

namespace Stl.ImmutableModel.Processing 
{
    public abstract class DispatchingNodeProcessorBase : NodeProcessorBase
    {
        protected DispatchingNodeProcessorBase(IModelProvider modelProvider, IUpdater updater) 
            : base(modelProvider, updater) { }

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
            var type = info.NodeDomainKey.Domain;
            return $"Process{type.ToMethodName()}NodeAsync";
        }

        protected virtual Task ProcessUnknownNodeAsync(INodeProcessingInfo info) => Task.CompletedTask;
    }
}
