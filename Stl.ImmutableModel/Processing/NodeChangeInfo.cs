using System;
using Newtonsoft.Json;
using Stl.ImmutableModel.Indexing;
using Stl.ImmutableModel.Updating;
using Stl.Text;

namespace Stl.ImmutableModel.Processing
{
    [Serializable]
    public struct NodeChangeInfo
    {
        public IModelUpdateInfo ModelUpdateInfo { get; }
        public INode Node { get; }
        public NodeLink NodeLink { get; }
        public NodeChangeType ChangeType { get; }

        [JsonConstructor]
        public NodeChangeInfo(IModelUpdateInfo modelUpdateInfo, INode node, NodeLink nodeLink, NodeChangeType changeType)
        {
            ModelUpdateInfo = modelUpdateInfo;
            Node = node;
            NodeLink = nodeLink;
            ChangeType = changeType;
        }

        public void Deconstruct(out IModelUpdateInfo modelUpdateInfo, out INode node, out NodeLink nodeLink, out NodeChangeType changeType)
        {
            modelUpdateInfo = ModelUpdateInfo;
            node = Node;
            nodeLink = NodeLink;
            changeType = ChangeType;
        }
    }
}
