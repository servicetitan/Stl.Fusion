using System.Collections.Generic;

namespace Stl.ImmutableModel.Internal
{
    public class NodeInfo
    {
        public string? Type { get; set; }
        public string? Key { get; set; }
        public Dictionary<string, object?>? Items { get; set; }
    }
}
