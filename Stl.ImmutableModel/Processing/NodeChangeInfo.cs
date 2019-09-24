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
        public SymbolList Path { get; }
        public NodeChangeType ChangeType { get; }

        [JsonConstructor]
        public NodeChangeInfo(IUpdateInfo updateInfo, INode node, SymbolList list, NodeChangeType changeType)
        {
            UpdateInfo = updateInfo;
            Node = node;
            Path = list;
            ChangeType = changeType;
        }

        public void Deconstruct(out IUpdateInfo updateInfo, out INode node, out SymbolList list, out NodeChangeType changeType)
        {
            updateInfo = UpdateInfo;
            node = Node;
            list = Path;
            changeType = ChangeType;
        }
    }
}
