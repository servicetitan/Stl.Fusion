using RestEase;
using Stl.Fusion.Bridge;

namespace Stl.Fusion.Client.RestEase
{
    [Header(FusionHeaders.RequestPublication, "1")]
    public interface IRestEaseReplicaClient : IReplicaClient
    { }
}
