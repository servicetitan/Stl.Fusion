using System;
using Newtonsoft.Json;
using Stl.ImmutableModel.Updating;
using Stl.Text;

namespace Stl.ImmutableModel.Processing
{
    [Serializable]
    public struct NodeChangeInfo
    {
        public IModelUpdateInfo ModelUpdateInfo { get; }
        public INode Node { get; }
        public SymbolList NodePath { get; }
        public NodeChangeType ChangeType { get; }

        [JsonConstructor]
        public NodeChangeInfo(IModelUpdateInfo modelUpdateInfo, INode node, SymbolList nodePath, NodeChangeType changeType)
        {
            ModelUpdateInfo = modelUpdateInfo;
            Node = node;
            NodePath = nodePath;
            ChangeType = changeType;
        }

        public void Deconstruct(out IModelUpdateInfo modelUpdateInfo, out INode node, out SymbolList nodePath, out NodeChangeType changeType)
        {
            modelUpdateInfo = ModelUpdateInfo;
            node = Node;
            nodePath = NodePath;
            changeType = ChangeType;
        }
    }
}
