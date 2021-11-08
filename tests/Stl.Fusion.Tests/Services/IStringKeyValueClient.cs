using RestEase;
using Stl.Fusion.Client;

namespace Stl.Fusion.Tests.Services;

public interface IKeyValueServiceClient<T> : IKeyValueService<T> { }

[RegisterRestEaseReplicaService(typeof(IKeyValueServiceClient<string>), Scope = ServiceScope.ClientServices)]
[RegisterRestEaseReplicaService(typeof(IKeyValueServiceClient<string>),
    IsCommandService = false, Scope = ServiceScope.Services)] // Just to ensure IsCommandService works
[BasePath("stringKeyValue")]
public interface IStringKeyValueClient
{
    [Get("tryGet")]
    Task<Option<string>> TryGet(string key, CancellationToken cancellationToken = default);
    [Get("get")]
    Task<JsonString> Get(string key, CancellationToken cancellationToken = default);
    [Post("set")]
    Task Set(string key, [Body] string value, CancellationToken cancellationToken = default);
    [Get("remove")]
    Task Remove(string key, CancellationToken cancellationToken = default);
    [Post("setCmd")]
    Task SetCmd([Body] IKeyValueService<string>.SetCommand cmd, CancellationToken cancellationToken = default);
    [Post("removeCmd")]
    Task RemoveCmd([Body] IKeyValueService<string>.RemoveCommand cmd, CancellationToken cancellationToken = default);
}
