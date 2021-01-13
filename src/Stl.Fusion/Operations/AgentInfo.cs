using Stl.OS;
using Stl.Text;

namespace Stl.Fusion.Operations
{
    public record AgentInfo(Symbol Id)
    {
        public AgentInfo() : this(RuntimeInfo.Process.MachinePrefixedId) { }
    }
}
