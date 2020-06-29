using RestEase;
using Stl.Fusion.Bridge;

namespace Stl.Fusion.Client
{
    public interface INoCacheReplicaService : IReplicaService
    {
        [Query("__nocache")]
        string NoCacheRandom { get; set; }
    }
}
