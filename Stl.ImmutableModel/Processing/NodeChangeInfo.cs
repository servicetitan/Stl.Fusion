using System;
using Newtonsoft.Json;
using Stl.ImmutableModel.Updating;

namespace Stl.ImmutableModel.Processing
{
    [Serializable]
    public struct NodeChangeInfo
    {
        public UpdateInfo UpdateInfo { get; }
        public INode Node { get; }
        public SymbolPath Path { get; }
        public NodeChangeType ChangeType { get; }

        [JsonConstructor]
        public NodeChangeInfo(UpdateInfo updateInfo, INode node, SymbolPath path, NodeChangeType changeType)
        {
            UpdateInfo = updateInfo;
            Node = node;
            Path = path;
            ChangeType = changeType;
        }

        public void Deconstruct(out UpdateInfo updateInfo, out INode node, out SymbolPath path, out NodeChangeType changeType)
        {
            updateInfo = UpdateInfo;
            node = Node;
            path = Path;
            changeType = ChangeType;
        }
    }
}
