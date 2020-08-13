using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Client;
using Stl.Fusion.Client.RestEase;

namespace Stl.Fusion.Tests.Services
{
    public interface IKeyValueServiceClient<T> : IKeyValueService<T> { }

    [RestEaseReplicaService(typeof(IKeyValueServiceClient<string>))]
    [BasePath("stringKeyValue")]
    public interface IStringKeyValueClient : IRestEaseReplicaClient
    {
        [Get("tryGet/{key}")]
        Task<Option<string>> TryGetAsync([Path] string key, CancellationToken cancellationToken = default);
        [Get("get/{key}")]
        Task<JsonString> GetAsync([Path] string key, CancellationToken cancellationToken = default);
        [Post("set/{key}")]
        Task SetAsync([Path] string key, [Body] string value, CancellationToken cancellationToken = default);
        [Get("remove/{key}")]
        Task RemoveAsync([Path] string key, CancellationToken cancellationToken = default);
    }
}
