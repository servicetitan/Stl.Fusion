using System;
using Newtonsoft.Json;
using Stl.ImmutableModel.Updating;

namespace Stl.ImmutableModel.Processing
{
    [Serializable]
    public abstract class NodeChangeInfo
    {
        [JsonIgnore] public abstract UpdateInfo UntypedUpdateInfo { get; }
        public INode Node { get; }
        public SymbolPath Path { get; }
        public NodeChangeType ChangeType { get; }

        protected NodeChangeInfo(INode node, SymbolPath path, NodeChangeType changeType)
        {
            Node = node;
            Path = path;
            ChangeType = changeType;
        }
    }

    public class NodeChangeInfo<TModel> : NodeChangeInfo
        where TModel : class, INode
    {
        public UpdateInfo<TModel> UpdateInfo { get; }
        public override UpdateInfo UntypedUpdateInfo => UpdateInfo;

        [JsonConstructor]
        public NodeChangeInfo(UpdateInfo<TModel> updateInfo, INode node, SymbolPath path, NodeChangeType changeType) 
            : base(node, path, changeType) 
            => UpdateInfo = updateInfo;
    }
}
