using RestEase;
using Stl.Fusion.Client;
using Stl.Fusion.Client.RestEase;

namespace Stl.Fusion.Tests.Services
{
    [RestEaseReplicaService()]
    [BasePath("stringKeyValue")]
    public interface IStringKeyValueClient : IRestEaseReplicaClient,
        IKeyValueService<string>
    { }
}
