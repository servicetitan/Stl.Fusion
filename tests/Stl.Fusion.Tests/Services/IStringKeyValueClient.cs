using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Client;
using Stl.Serialization;

namespace Stl.Fusion.Tests.Services
{
    public interface IKeyValueServiceClient<T> : IKeyValueService<T> { }

    [RestEaseReplicaService(typeof(IKeyValueServiceClient<string>), Scope = ServiceScope.ClientServices)]
    [RestEaseReplicaService(typeof(IKeyValueServiceClient<string>),
        IsCommandService = false, Scope = ServiceScope.Services)] // Just to ensure IsCommandService works
    [BasePath("stringKeyValue")]
    public interface IStringKeyValueClient
    {
        [Get("tryGet/{key}")]
        Task<Option<string>> TryGetAsync([Path] string key, CancellationToken cancellationToken = default);
        [Get("get/{key}")]
        Task<JsonString> GetAsync([Path] string key, CancellationToken cancellationToken = default);
        [Post("set/{key}")]
        Task SetAsync([Path] string key, [Body] string value, CancellationToken cancellationToken = default);
        [Get("remove/{key}")]
        Task RemoveAsync([Path] string key, CancellationToken cancellationToken = default);
        [Post("setCommand")]
        Task SetCommandAsync([Body] IKeyValueService<string>.SetCommand cmd, CancellationToken cancellationToken = default);
        [Post("removeCommand")]
        Task RemoveCommandAsync([Body] IKeyValueService<string>.RemoveCommand cmd, CancellationToken cancellationToken = default);
    }
}
