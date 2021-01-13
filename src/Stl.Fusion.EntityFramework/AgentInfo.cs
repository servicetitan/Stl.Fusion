using Stl.OS;
using Stl.Text;

namespace Stl.Fusion.EntityFramework
{
    public record AgentInfo(Symbol Id)
    {
        public AgentInfo() : this(RuntimeInfo.Process.MachinePrefixedId) { }
    }
}
