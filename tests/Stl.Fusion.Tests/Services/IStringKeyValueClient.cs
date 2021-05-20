using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Client;
using Stl.Serialization;

namespace Stl.Fusion.Tests.Services
{
    public interface IKeyValueServiceClient<T> : IKeyValueService<T> { }

    [RegisterRestEaseReplicaService(typeof(IKeyValueServiceClient<string>), Scope = ServiceScope.ClientServices)]
    [RegisterRestEaseReplicaService(typeof(IKeyValueServiceClient<string>),
        IsCommandService = false, Scope = ServiceScope.Services)] // Just to ensure IsCommandService works
    [BasePath("stringKeyValue")]
    public interface IStringKeyValueClient
    {
        [Get("tryGet/{key}")]
        Task<Option<string>> TryGet([Path] string key, CancellationToken cancellationToken = default);
        [Get("get/{key}")]
        Task<JsonString> Get([Path] string key, CancellationToken cancellationToken = default);
        [Post("set/{key}")]
        Task Set([Path] string key, [Body] string value, CancellationToken cancellationToken = default);
        [Get("remove/{key}")]
        Task Remove([Path] string key, CancellationToken cancellationToken = default);
        [Post("setCmd")]
        Task SetCmd([Body] IKeyValueService<string>.SetCommand cmd, CancellationToken cancellationToken = default);
        [Post("removeCmd")]
        Task RemoveCmd([Body] IKeyValueService<string>.RemoveCommand cmd, CancellationToken cancellationToken = default);
    }
}
