using System.Threading;
using Stl.OS;
using Stl.Text;

namespace Stl.Fusion.Operations
{
    public record AgentInfo(Symbol Id)
    {
        private static long _nextId = 0;
        private static long GetNextId() => Interlocked.Increment(ref _nextId);

        public AgentInfo()
            : this($"{RuntimeInfo.Process.MachinePrefixedId}:{GetNextId()}")
        { }
    }
}
