using System;
using Newtonsoft.Json;
using Stl.ImmutableModel.Updating;

namespace Stl.ImmutableModel.Processing
{
    [Serializable]
    public struct NodeChangeInfo
    {
        public IUpdateInfo UpdateInfo { get; }
        public INode Node { get; }
        public SymbolList NodePath { get; }
        public NodeChangeType ChangeType { get; }

        [JsonConstructor]
        public NodeChangeInfo(IUpdateInfo updateInfo, INode node, SymbolList nodePath, NodeChangeType changeType)
        {
            UpdateInfo = updateInfo;
            Node = node;
            NodePath = nodePath;
            ChangeType = changeType;
        }

        public void Deconstruct(out IUpdateInfo updateInfo, out INode node, out SymbolList nodePath, out NodeChangeType changeType)
        {
            updateInfo = UpdateInfo;
            node = Node;
            nodePath = NodePath;
            changeType = ChangeType;
        }
    }
}
